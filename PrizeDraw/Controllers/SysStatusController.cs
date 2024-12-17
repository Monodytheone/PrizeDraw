using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using PrizeDraw.Dto.Response;
using PrizeDraw.Entities;
using PrizeDraw.Services;
using SqlSugar;

namespace PrizeDraw.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class SysStatusController : ControllerBase
{
    private readonly ISqlSugarClient _db;
    private readonly PrizeListPageService _prizeListPageService;
    private readonly IHubContext<PublicHub> _publicHubContext;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;

    public SysStatusController(ISqlSugarClient db, PrizeListPageService prizeListPageService, IHubContext<PublicHub> publicHubContext, IConfiguration configuration, IMemoryCache memoryCache)
    {
        _db = db;
        _prizeListPageService = prizeListPageService;
        _publicHubContext = publicHubContext;
        _configuration = configuration;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// 获取系统状态
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SysStatusDto>> Get()
    {
        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();
        SysStatus sysStatus = statusEntity.SysStatus;
        bool hideEmployeeInfo = statusEntity.HideEmployeeInfo;

        if (sysStatus == SysStatus.NotStart)
        {
            List<RafflePrizeEntity> rafflePrizes = await _db.Queryable<RafflePrizeEntity>().ToListAsync();
            List<EmployeeEntity> employees = await _db.Queryable<EmployeeEntity>().ToListAsync();
            SysStatusDto res = new()
            {
                SysStatus = sysStatus,
                HideEmployeeInfo = hideEmployeeInfo,
                NotStart_1 = new SysStatusDto_NotStart_1
                {
                    RafflePrizes = rafflePrizes,
                    Employees = employees
                }
            };
            return Ok(res);
        }
        else if (sysStatus == SysStatus.Started || sysStatus == SysStatus.Finished)
        {
            PrizeListPageDataDto prizeListPageData = await _prizeListPageService.GetPrizeListPageDataAsync();
            SysStatusDto res = new()
            {
                SysStatus = sysStatus,
                HideEmployeeInfo = hideEmployeeInfo,
                Started_2_5 = prizeListPageData
            };
            return Ok(res);
        }
        else if (sysStatus == SysStatus.Rolling)
        {
            List<EmployeeEntity> rollingEmployees = await _db.Queryable<EmployeeEntity>()
                .Where(e => e.HasWon == false).ToListAsync();
            RollingRafflePrize rollingRafflePrize = await _db.Queryable<RollingRafflePrize>().SingleAsync();
            SysStatusDto res = new()
            {
                SysStatus = sysStatus,
                HideEmployeeInfo = hideEmployeeInfo,
                Rolling_3 = new RollingPageDataDto
                {
                    RollingEmployees = rollingEmployees,
                    RollingRafflePrize = rollingRafflePrize
                }
            };
            return Ok(res);
        }
        else if (sysStatus == SysStatus.RollStop)
        {
            RollingRafflePrize rollingRafflePrize = await _db.Queryable<RollingRafflePrize>().SingleAsync();
            RollStopPageDataDto rollStopPageData = new()
            {
                RafflePrize = rollingRafflePrize,
                CurrentWinner = await _db.Queryable<CurrentWinner>().SingleAsync(),
                PrizeRecordOfCurrentRafflePrize = await _db.Queryable<PrizeRecordEntity>()
                    .Where(r => r.RafflePrizeId == rollingRafflePrize.Id)
                    .OrderBy(r => r.WinTime)
                    .ToListAsync(),
            };
            SysStatusDto res = new()
            {
                SysStatus = sysStatus,
                HideEmployeeInfo = hideEmployeeInfo,
                StopRoll_4 = rollStopPageData
            };
            return Ok(res);
        }

        SysStatusDto res1 = new()
        {
            SysStatus = sysStatus
        };
        return Ok(res1);
    }

    /// <summary>
    /// 获取系统状态-移动端
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public Task<ActionResult<SysStatusDto>> GetFromCache()
    {
        return _memoryCache.GetOrCreateAsync("PrizeDraw:SysStatus", (cacheEntry) =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2);
            return Get();
        })!;
    }

    /// <summary>
    /// 開始抽獎流程-系統狀態變為2-跳转到奖项列表页面
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PrizeListPageDataDto>> Start()
    {
        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();

        if (statusEntity.SysStatus != SysStatus.NotStart)
        {
            return BadRequest($"系统状态为{statusEntity.SysStatus}，不得Start");
        }

        if (await _db.Queryable<RafflePrizeEntity>().CountAsync() == 0)
        {
            return BadRequest("未设置奖项");
        }

        if (await _db.Queryable<EmployeeEntity>().CountAsync() == 0)
        {
            return BadRequest("未导入员工列表");
        }

        statusEntity.SysStatus = SysStatus.Started;
        await _db.Updateable(statusEntity).ExecuteCommandAsync();

        PrizeListPageDataDto prizeListPageData = await _prizeListPageService.GetPrizeListPageDataAsync();

        // 推送SignalR消息
        await _publicHubContext.Clients.All.SendAsync("Start", prizeListPageData);

        return Ok(prizeListPageData);
    }

    /// <summary>
    /// 開始滾動
    /// </summary>
    /// <param name="rafflePrizeId">奖项Id</param>
    [HttpPost]
    public async Task<ActionResult<RollingPageDataDto>> StartRoll(string rafflePrizeId)
    {
        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();

        if (statusEntity.SysStatus != SysStatus.Started && statusEntity.SysStatus != SysStatus.RollStop)
        {
            return BadRequest($"系统状态为{statusEntity.SysStatus}，不得开始滚动");
        }

        RafflePrizeEntity rafflePrize = await _db.Queryable<RafflePrizeEntity>().SingleAsync(r => r.Id == rafflePrizeId);
        if (rafflePrize == null)
        {
            return BadRequest($"奖项 '{rafflePrizeId}' 不存在");
        }

        if (rafflePrize.DrawsLeft <= 0)
        {
            return BadRequest($"奖项 '{rafflePrize.PrizeName}' 无剩余可抽数量");
        }

        List<EmployeeEntity> rollingEmployees = await _db.Queryable<EmployeeEntity>()
            .Where(e => e.HasWon == false)
            .ToListAsync();
        if (rollingEmployees.Count == 0)
        {
            return BadRequest("所有員工都已中獎");
        }

        // 清除CurrentWinner
        await _db.Deleteable<CurrentWinner>().ExecuteCommandAsync();

        RollingRafflePrize rollingRafflePrize = new()
        {
            Id = rafflePrize.Id,
            PrizeName = rafflePrize.PrizeName,
            Quantity = rafflePrize.Quantity,
            PrizeAmount = rafflePrize.PrizeAmount,
            DrawnCount = rafflePrize.DrawnCount,
            DrawsLeft = rafflePrize.DrawsLeft
        };        
        await _db.Deleteable<RollingRafflePrize>().ExecuteCommandAsync();
        await _db.Insertable(rollingRafflePrize).ExecuteCommandAsync();

        statusEntity.SysStatus = SysStatus.Rolling;
        await _db.Updateable(statusEntity).ExecuteCommandAsync();

        RollingPageDataDto res = new()
        {
            RollingEmployees = rollingEmployees,
            RollingRafflePrize = rollingRafflePrize
        };

        await _publicHubContext.Clients.All.SendAsync("StartRoll", res);

        return Ok(res);
    }

    /// <summary>
    /// 停止滚动
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RollStopPageDataDto>> StopRoll()
    {
        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();
        if (statusEntity.SysStatus != SysStatus.Rolling)
        {
            return BadRequest($"系统状态为{statusEntity.SysStatus}，不得停止滚动");
        }

        RollingRafflePrize rollingRafflePrize = await _db.Queryable<RollingRafflePrize>().SingleAsync()
            ?? throw new Exception("未找到RollingRafflePrize");

        RafflePrizeEntity rafflePrize = await _db.Queryable<RafflePrizeEntity>().SingleAsync(r => r.Id == rollingRafflePrize.Id)
            ?? throw new Exception("未找到RafflePrize");
        if (rafflePrize.DrawsLeft <= 0)
        {
            throw new Exception($"奖项 '{rafflePrize.PrizeName}' 剩余可抽数量为 {rafflePrize.DrawsLeft}");
        }

        List<EmployeeEntity> rollingEmployees = await _db.Queryable<EmployeeEntity>()
            .Where(e => e.HasWon == false)
            .ToListAsync();

        // 真随机
        Random random = new(Guid.NewGuid().GetHashCode());
        int randomIndex = random.Next(0, rollingEmployees.Count);
        EmployeeEntity winnerEmployee = rollingEmployees[randomIndex];
        winnerEmployee.HasWon = true;
        await _db.Updateable(winnerEmployee).ExecuteCommandAsync();

        CurrentWinner currentWinner = new()
        {
            UserId = winnerEmployee.UserId,
            UserName = winnerEmployee.UserName,
            Department = winnerEmployee.Department
        };
        await _db.Deleteable<CurrentWinner>().ExecuteCommandAsync();
        await _db.Insertable(currentWinner).ExecuteCommandAsync();

        // 构造中奖记录
        PrizeRecordEntity prizeRecord = new()
        {
            Id = SnowFlakeSingle.Instance.NextId().ToString(),
            RafflePrizeId = rafflePrize.Id,
            RafflePrizeName = rafflePrize.PrizeName,
            PrizeAmount = rafflePrize.PrizeAmount,
            WinnerId = winnerEmployee.UserId,
            WinnerName = winnerEmployee.UserName,
            WinnerDepartment = winnerEmployee.Department,
            WinTime = DateTime.Now
        };
        await _db.Insertable(prizeRecord).ExecuteCommandAsync();

        // 更新奖项。
        rafflePrize.DrawnCount++;
        rafflePrize.DrawsLeft--;
        await _db.Updateable(rafflePrize).ExecuteCommandAsync();

        // RollingRafflePrize不删除而是更新，滚定停止页面需要用到
        rollingRafflePrize.DrawnCount++;
        rollingRafflePrize.DrawsLeft--;
        await _db.Updateable(rollingRafflePrize).ExecuteCommandAsync();

        statusEntity.SysStatus = SysStatus.RollStop;
        await _db.Updateable(statusEntity).ExecuteCommandAsync();

        List<PrizeRecordEntity> recordsOfCurrentRafflePrize = await _db.Queryable<PrizeRecordEntity>()
            .Where(r => r.RafflePrizeId == rafflePrize.Id)
            .OrderBy(r => r.WinTime)
            .ToListAsync();

        RollStopPageDataDto res = new()
        {
            RafflePrize = rollingRafflePrize,
            CurrentWinner = currentWinner,
            PrizeRecordOfCurrentRafflePrize = recordsOfCurrentRafflePrize
        };

        await _publicHubContext.Clients.All.SendAsync("StopRoll", res);

        return Ok(res);
    }

    /// <summary>
    /// （从滚动页面或停止滚动页面）返回到奖项列表页面
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PrizeListPageDataDto>> BackToPrizeListPage()
    {
        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();
        if (statusEntity.SysStatus != SysStatus.Rolling && statusEntity.SysStatus != SysStatus.RollStop)
        {
            return BadRequest($"系统状态为{statusEntity.SysStatus}，不得BackToPrizeListPage");
        }

        // 清除RollingRafflePrize和CurrentWinner
        await _db.Deleteable<CurrentWinner>().ExecuteCommandAsync();
        await _db.Deleteable<RollingRafflePrize>().ExecuteCommandAsync();



        PrizeListPageDataDto prizeListPageData = await _prizeListPageService.GetPrizeListPageDataAsync();

        // 更改系统状态
        bool hasFinished = await _db.Queryable<RafflePrizeEntity>().Where(r => r.DrawsLeft > 0).CountAsync() == 0;
        statusEntity.SysStatus = hasFinished ? SysStatus.Finished : SysStatus.Started;
        await _db.Updateable(statusEntity).ExecuteCommandAsync();

        // 推送消息
        string messageMethod = hasFinished ? "Finish" : "BackToPrizeListPage";
        await _publicHubContext.Clients.All.SendAsync(messageMethod, prizeListPageData);

        return Ok(prizeListPageData);
    }

    /// <summary>
    /// 重置系统
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> ResetSystem(string password)
    {
        if (password != _configuration["SysRestPassword"])
        {
            return BadRequest("請提供正確的重置密碼");
        }

        await _db.Deleteable<CurrentWinner>().ExecuteCommandAsync();
        await _db.Deleteable<EmployeeEntity>().ExecuteCommandAsync();
        await _db.Deleteable<PrizeRecordEntity>().ExecuteCommandAsync();
        await _db.Deleteable<RafflePrizeEntity>().ExecuteCommandAsync();
        await _db.Deleteable<RollingRafflePrize>().ExecuteCommandAsync();

        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();
        statusEntity.SysStatus = SysStatus.NotStart;
        await _db.Updateable(statusEntity).ExecuteCommandAsync();

        await _publicHubContext.Clients.All.SendAsync("SystemReset");

        return Ok();
    }

    /// <summary>
    /// 显示/隐藏员工信息
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> ChangeHideEmployeeInfoStatus(bool hideEmployeeInfo)
    {
        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();

        statusEntity.HideEmployeeInfo = hideEmployeeInfo;
        await _db.Updateable(statusEntity).ExecuteCommandAsync();

        await _publicHubContext.Clients.All.SendAsync("ChangeHideEmployeeInfoStatus", hideEmployeeInfo);

        return Ok();
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PrizeDraw.Dto.Request;
using PrizeDraw.Dto.Response;
using PrizeDraw.Entities;
using PrizeDraw.Services;
using SqlSugar;

namespace PrizeDraw.Controllers;

/// <summary>
/// 数据设置
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class SetDataController : ControllerBase
{
    private readonly ISqlSugarClient _db;
    private readonly IHubContext<PublicHub> _publicHubContext;
    private readonly PrizeListPageService _prizeListPageService;


    private readonly IValidator<SetRafflePrizeDto> _setRafflePrizeValidator;
    private readonly IValidator<AddBonusPrizeDto> _addBonusPrizeValidator;

    public SetDataController(ISqlSugarClient db, IValidator<SetRafflePrizeDto> setRafflePrizeValidator, IValidator<AddBonusPrizeDto> addBonusPrizeValidator, IHubContext<PublicHub> publicHubContext, PrizeListPageService prizeListPageService)
    {
        _db = db;
        _setRafflePrizeValidator = setRafflePrizeValidator;
        _addBonusPrizeValidator = addBonusPrizeValidator;
        _publicHubContext = publicHubContext;
        _prizeListPageService = prizeListPageService;
    }

    /// <summary>
    /// 设置奖项
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<List<RafflePrizeEntity>>> SetRafflePrize(SetRafflePrizeDto input)
    {
        var validationResult = _setRafflePrizeValidator.Validate(input);
        if (validationResult.IsValid == false)
        {
            return BadRequest(string.Join(' ', validationResult.Errors.Select(error => error.ErrorMessage)));
        }

        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();
        if (statusEntity.SysStatus != SysStatus.NotStart)
        {
            return BadRequest("抽奖已开始，不得重設奖项。");
        }

        await _db.Deleteable<RafflePrizeEntity>().ExecuteCommandAsync();

        List<RafflePrizeEntity> rafflePrizes = input.Items.Select(item => new RafflePrizeEntity
        {
            Id = SnowFlakeSingle.Instance.NextId().ToString(),
            PrizeName = item.PrizeName,
            Quantity = item.Quantity,
            PrizeAmount = item.PrizeAmount,
            DrawnCount = 0,
            DrawsLeft = item.Quantity,
        }).ToList();

        await _db.Insertable(rafflePrizes).ExecuteCommandAsync();

        return rafflePrizes;
    }

    /// <summary>
    /// 临时加奖
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<List<RafflePrizeEntity>>> AddBonusPrize(AddBonusPrizeDto input)
    {
        var validationResult = _addBonusPrizeValidator.Validate(input);
        if (validationResult.IsValid == false)
        {
            return BadRequest(string.Join(' ', validationResult.Errors.Select(error => error.ErrorMessage)));
        }

        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();
        if (statusEntity.SysStatus != SysStatus.Started && statusEntity.SysStatus != SysStatus.Finished)
        {
            return BadRequest($"当前系统状态为 '{statusEntity.SysStatus}'，不得临时加奖");
        }

        RafflePrizeEntity bonusPrize = new()
        {
            Id = SnowFlakeSingle.Instance.NextId().ToString(),
            PrizeName = input.PrizeName,
            Quantity = input.Quantity,
            PrizeAmount = input.PrizeAmount,
            DrawnCount = 0,
            DrawsLeft = input.Quantity,
        };
        await _db.Insertable(bonusPrize).ExecuteCommandAsync();

        if (statusEntity.SysStatus == SysStatus.Finished)
        {
            statusEntity.SysStatus = SysStatus.Started;
            await _db.Updateable(statusEntity).ExecuteCommandAsync();
        }

        // 推送SignalR消息
        PrizeListPageDataDto prizeListPageData = await _prizeListPageService.GetPrizeListPageDataAsync();
        await _publicHubContext.Clients.All.SendAsync("Start", prizeListPageData);

        // 返回最新的奖项列表
        List<RafflePrizeEntity> rafflePrizeList = await _db.Queryable<RafflePrizeEntity>().ToListAsync();
        return rafflePrizeList;
    }


    /// <summary>
    /// 上传员工信息
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<List<EmployeeEntity>>> UploadEmployees(IFormFile file)
    {
        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();
        if (statusEntity.SysStatus != SysStatus.NotStart)
        {
            return BadRequest("抽奖已开始，不得重新上傳員工信息。");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("请上传文件");
        }

        List<EmployeeEntity> employeeList = [];

        #region 从文件中讀取員工列表
        var userIdSet = new HashSet<string>();
        try
        {
            using var stream = file.OpenReadStream();

            IWorkbook workbook = new XSSFWorkbook(stream);
            ISheet sheet = workbook.GetSheetAt(0); // Assuming data is in the first sheet

            // Start reading from the second row (index 1) to skip headers
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);

                if (row == null)
                {
                    return BadRequest($"第 {rowIndex + 1} 行为空。");
                }

                // Read cell values for EmployeeEntity properties
                var userId = row.GetCell(0)?.ToString()?.Trim();
                var userName = row.GetCell(1)?.ToString()?.Trim();
                var department = row.GetCell(2)?.ToString()?.Trim();

                var missingCells = new List<string>();

                if (string.IsNullOrEmpty(userId))
                {
                    missingCells.Add("工号 (第 1 列)");
                }
                if (string.IsNullOrEmpty(userName))
                {
                    missingCells.Add("姓名 (第 2 列)");
                }
                if (string.IsNullOrEmpty(department))
                {
                    missingCells.Add("部门 (第 3 列)");
                }

                if (missingCells.Any())
                {
                    return BadRequest($"第 {rowIndex + 1} 行包含空单元格: {string.Join("，", missingCells)}。");
                }

                if (userIdSet.Contains(userId))
                {
                    return BadRequest($"第 {rowIndex + 1} 行发现重复的工号: {userId}。");
                }

                userIdSet.Add(userId);

                employeeList.Add(new EmployeeEntity
                {
                    UserId = userId,
                    UserName = userName,
                    Department = department,
                    HasWon = false // Default to false
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"处理文件时发生错误: {ex.Message}");
        }
        #endregion

        await _db.Deleteable<EmployeeEntity>().ExecuteCommandAsync();

        await _db.Insertable(employeeList).ExecuteCommandAsync();

        return Ok(employeeList);
    }

    /// <summary>
    /// 设置公证人
    /// </summary>
    /// <param name="notary">公证人</param>
    [HttpPost]
    public async Task<ActionResult> SetNotary(string notary)
    {
        if (string.IsNullOrWhiteSpace(notary))
        {
            return BadRequest("请输入公证人");
        }

        SysStatusEntity statusEntity = await _db.Queryable<SysStatusEntity>().SingleAsync();
        if(statusEntity.SysStatus != SysStatus.NotStart)
        {
            return BadRequest("抽奖已开始，不得变更公证人");
        }

        statusEntity.Notary = notary;
        await _db.Updateable(statusEntity).ExecuteCommandAsync();

        return Ok();
    }
}

using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Reflection;

namespace PrizeDraw.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class CodeFirstController : ControllerBase
{
    private readonly ISqlSugarClient _db;
    private readonly IConfiguration _configuration;

    public CodeFirstController(ISqlSugarClient db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpGet]
    public string ShowDbDiff()
    {
        Console.WriteLine(_configuration["ConnectionString"]);

        Assembly assembly = typeof(Program).Assembly;

        Type[] sugarTableTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(SugarTable), true).Length > 0)
            .Where(t => t.Name != "SysUser")
        .ToArray();

        string diffString = _db.CodeFirst.GetDifferenceTables(sugarTableTypes).ToDiffString();

        return diffString;
    }

    [HttpPost]
    public void InitTables()
    {
        Assembly assembly = typeof(Program).Assembly;

        List<Type> types = assembly.GetTypes()
                .Where(x => x.GetCustomAttribute<SugarTable>() != null)
                .ToList();

        if (types.Count > 0)
        {
            _db.CopyNew().CodeFirst.InitTables(types.ToArray());
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace SengokuScroll.WebApi.Controllers;

//[ApiController]
[Route("[controller]")]
public class BaseController<T> : Controller where T : BaseController<T>
{
    protected readonly ILogger<T> logger;

    public BaseController(ILogger<T> logger)
    {
        this.logger = logger;
    }
}

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DatingApp.API.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace DatingApp.API.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // do smt after action has been executed
            // waiting until the action has been completed and then using 'resultContext'
            var resultContext = await next();

            var userId = int.Parse(resultContext.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier).Value);
            // get instance of service 'IDatingRepository'
            var repo = resultContext.HttpContext.RequestServices.GetService<IDatingRepository>();
            // get user with id
            var user = await repo.GetUser(userId, true);
            // update last active value
            user.LastActive = DateTime.Now;
            // save to database
            await repo.SaveAll();
        }
    }
}
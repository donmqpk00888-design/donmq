using API._Repositories;
using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helper.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class TransactionalAttribute : ActionFilterAttribute
    {
        private readonly int maxRetries = 3; // Times
        private readonly int delay = 3000; // Millisecond
        public TransactionalAttribute() { }
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var dBContext = context.HttpContext.RequestServices.GetService<DBContext>();
            var _repositoryAccessor = new RepositoryAccessor<DBContext>(dBContext);
            int currentAttempt = 0;
            do
            {
                currentAttempt++;
                try
                {
                    if (!await _repositoryAccessor.IsDatabaseLockedAsync())
                    {
                        await next();
                        break;
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(delay));
                }
                catch (Exception)
                {
                    context.Result = new ObjectResult("An error occurred while connecting to the server")
                    { StatusCode = StatusCodes.Status500InternalServerError };
                    return;
                }
            } while (currentAttempt <= maxRetries);
            context.Result = new ConflictObjectResult("A database transaction is already active. Please try againt later");
            return;
        }
    }
}


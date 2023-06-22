﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestaurantAPI.Exceptions;
using System;
using System.Threading.Tasks;

namespace RestaurantAPI.Middleware
{
    public class ErrorHandlingMiddleware : IMiddleware
    {
        private readonly ILogger _logger;

        public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
        {
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next.Invoke(context);

            }catch(ForbidException forbidException)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync(forbidException.Message);
                
            }
            catch(BadRequestException badrequestexception)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(badrequestexception.Message);
            }
            catch(NotFoundException notFoundException)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(notFoundException.Message);
            }
            catch(ArgumentNullException argumentNullException)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(argumentNullException.Message);
            }
            catch(Exception e)
            {
                _logger.LogError(e, e.Message);
                context.Response.StatusCode= 500;
                 await context.Response.WriteAsync("Something went wrong");

            }
            

        }
    }
}

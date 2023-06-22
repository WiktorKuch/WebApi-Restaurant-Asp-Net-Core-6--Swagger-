﻿using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RestaurantAPI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RestaurantAPI.IntegrationTests
{
    public class StartupTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly List<Type> _controllerTypes;
        private readonly WebApplicationFactory<Program> _factory;

        public StartupTests()
        {
            _controllerTypes = typeof(Program)
                .Assembly
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)))
                .ToList();


            _factory = new WebApplicationFactory < Program >().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    _controllerTypes.ForEach(c => services.AddScoped(c));
                });

            });
        }

        [Fact]
        public void ConfigureServices_ForControllers_RegistersAllDependencies()
        {
            var scopeFactory = _factory.Services.GetService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();

            //assert
            _controllerTypes.ForEach(t =>
            {
                var controller = scope.ServiceProvider.GetService(t);
                controller.Should().NotBeNull();
            });
            
             
            
        }
    }
}

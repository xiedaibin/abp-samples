using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement;
using ProductManagement.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using MsDemo.Shared;
using Swashbuckle.AspNetCore.Swagger;
using Volo.Abp;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.Security.Claims;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.Threading;
using System.IO;

namespace ProductService.Host
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(AbpEventBusRabbitMqModule),
        typeof(AbpEntityFrameworkCoreSqlServerModule),
        typeof(AbpAuditLoggingEntityFrameworkCoreModule),
        typeof(AbpPermissionManagementEntityFrameworkCoreModule),
        typeof(AbpSettingManagementEntityFrameworkCoreModule),
        typeof(ProductManagementApplicationModule),
        typeof(ProductManagementHttpApiModule),
        typeof(ProductManagementEntityFrameworkCoreModule),
        typeof(AbpAspNetCoreMultiTenancyModule),
        typeof(AbpTenantManagementEntityFrameworkCoreModule)
        )]
    public class ProductServiceHostModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            
            //Configure<AbpAspNetCoreMvcOptions>(options =>
            //{
            //    options
            //    .ConventionalControllers
            //    .Create(typeof(ProductManagementApplicationModule).Assembly);
            //});

            Configure<AbpMultiTenancyOptions>(options =>
            {
                options.IsEnabled = MsDemoConsts.IsMultiTenancyEnabled;
            });

            context.Services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = configuration["AuthServer:Authority"];
                    options.ApiName = configuration["AuthServer:ApiName"];
                    options.RequireHttpsMetadata = false;
                });
            var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);//获取应用程序所在目录（绝对，不受工作目录影响，建议采用此方法获取路径）
            var xmlPath = Path.Combine(basePath, "ProductManagement.HttpApi.xml");   //  添加 swagger xml 注释  这个xml文件开始是不存在的写上项目名.xml即可
            var xmlpath1 = Path.Combine(basePath, "ProductManagement.Application.Contracts.xml");
            context.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Product Service API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                //ProductManagement.HttpApi.xml
                options.IncludeXmlComments(xmlPath);
                options.IncludeXmlComments(xmlpath1);
            });

            Configure<AbpLocalizationOptions>(options =>
            {
                options.Languages.Add(new LanguageInfo("en", "en", "English"));
            });

            Configure<AbpDbContextOptions>(options =>
            {
                options.UseSqlServer();
            });

            context.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:Configuration"];
            });

            Configure<AbpAuditingOptions>(options =>
            {
                options.IsEnabledForGetRequests = true;
                options.ApplicationName = "ProductService";
            });

            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            context.Services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "MsDemo-DataProtection-Keys");
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();

            app.UseCorrelationId();
            app.UseVirtualFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAbpClaimsMap();

            if (MsDemoConsts.IsMultiTenancyEnabled)
            {
                app.UseMultiTenancy();
            }

            app.UseAbpRequestLocalization(); //TODO: localization?
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API");
            });
            app.UseAuditing();
            app.UseConfiguredEndpoints();

            //TODO: Problem on a clustered environment
            AsyncHelper.RunSync(async () =>
            {
                using (var scope = context.ServiceProvider.CreateScope())
                {
                    await scope.ServiceProvider
                        .GetRequiredService<IDataSeeder>()
                        .SeedAsync();
                }
            });
        }
    }
}

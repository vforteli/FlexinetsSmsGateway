using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flexinets.Core.Communication.Sms;
using Flexinets.Core.Database.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlexinetsSmsGateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<FlexinetsContext>(options => options.UseSqlServer(Configuration.GetConnectionString("FlexinetsContext")));
            services.AddTransient<ISmsGateway>(o => new SMSGatewayTwilio(
                Configuration["Twilio:deliveryreporturl"],
                Configuration["Twilio:accountsid"],
                Configuration["Twilio:authtoken"]));
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}

using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using Microsoft.AspNet.SignalR.Hubs;
using System.Diagnostics;
using Microsoft.Owin.Security.Cookies;
using System.Web.Security;
using System.Security.Claims;
using vfs.synchronizer.common;
using System.Collections.Generic;

namespace vfs.synchronizer.server {
    class Program {
        static void Main(string[] args) {
            string url = "http://localhost:8080/";
            using (WebApp.Start(url)) {
                Console.WriteLine("Server running on {0}", url);
                Console.ReadLine();
            }
        }
    }

    class Startup {
        public void Configuration(IAppBuilder app) {
            app.UseCors(CorsOptions.AllowAll);

            var options = new CookieAuthenticationOptions() {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                LoginPath = CookieAuthenticationDefaults.LoginPath,
                LogoutPath = CookieAuthenticationDefaults.LogoutPath,
            };

            app.UseCookieAuthentication(options);

            app.Use(async (context, next) => {
                if (context.Request.Path.Value.Contains(options.LoginPath.Value)) {
                    var form = await context.Request.ReadFormAsync();
                    var username = form["UserName"].ToString();
                    var password = form["Password"].ToString();

                    if (!ValidateUser(username, password)) {
                        return;
                    }

                    var claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.Name, username));
                    var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);
                    context.Authentication.SignIn(id);
                    return;
                }

                await next();
            });

            app.MapSignalR();
        }

        private bool ValidateUser(string username, string password) {
            Console.WriteLine("ValidateUser({0}, {1})", username, password);
            var db = new JCDSynchronizerDatabase();
            var success = db.Login(username, password) > 0;
            db.CloseDbConnection();
            return success;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class AuthorizeClaimsAttribute : AuthorizeAttribute {
        protected override bool UserAuthorized(System.Security.Principal.IPrincipal user) {
            if (user == null) {
                throw new ArgumentNullException("user");
            }

            var principal = (ClaimsPrincipal)user;

            if (principal != null) {
                Claim authenticated = principal.FindFirst(ClaimTypes.Authentication);
                return authenticated.Value == "true" ? true : false;
            }
            else {
                return false;
            }
        }
    }
}

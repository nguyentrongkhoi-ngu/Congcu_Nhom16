using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace CinemaBooking.Middlewares
{
    public class AdminRestrictionMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminRestrictionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.User;
            var path = context.Request.Path.Value;

            // 1. ADMISTRATION PROTECTION (USER -> ADMIN)
            // If the user tries to access /Admin but is not an Admin
            if (path.StartsWith("/Admin", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!user.Identity.IsAuthenticated)
                {
                    // Not logged in? Go to login
                    context.Response.Redirect("/Account/Login?returnUrl=" + System.Net.WebUtility.UrlEncode(path));
                    return;
                }
                
                if (!user.IsInRole("Admin"))
                {
                    // Logged in as regular user but trying to access admin area? Deny access.
                    context.Response.Redirect("/Account/AccessDenied");
                    return;
                }
            }

            // 2. PUBLIC SITE PROTECTION (ADMIN -> DASHBOARD)
            // If the user is an Admin, they should only be in /Admin or /Account or static files
            if (user.Identity.IsAuthenticated && user.IsInRole("Admin"))
            {
                // Exceptions: 
                // 1. Admin area (handled above)
                // 2. Account controller (logout, etc.)
                // 3. Static files/uploads
                // 4. SignalR Hubs
                
                bool isAdminArea = path.StartsWith("/Admin", System.StringComparison.OrdinalIgnoreCase);
                bool isAccountAction = path.StartsWith("/Account", System.StringComparison.OrdinalIgnoreCase);
                bool isUploads = path.StartsWith("/uploads", System.StringComparison.OrdinalIgnoreCase);
                bool isHub = path.StartsWith("/bookingHub", System.StringComparison.OrdinalIgnoreCase);
                
                // Also check for static files (simple check by extension)
                bool isStaticFile = path.Contains(".") && 
                                   (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".jpg") || 
                                    path.EndsWith(".png") || path.EndsWith(".gif") || path.EndsWith(".svg") ||
                                    path.EndsWith(".woff") || path.EndsWith(".woff2") || path.EndsWith(".ttf") ||
                                    path.EndsWith(".ico"));

                if (!isAdminArea && !isAccountAction && !isUploads && !isHub && !isStaticFile)
                {
                    // Redirect Admin to their dashboard if they try to access public pages
                    context.Response.Redirect("/Admin/Home/Index");
                    return;
                }
            }

            await _next(context);
        }
    }
}

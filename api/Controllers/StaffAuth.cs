using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    internal static class StaffAuth
    {
        /// <summary>
        /// Checks if the request is from a staff member, if not returns true and a 403 result
        /// </summary>
        internal static bool IsNotStaff(HttpRequest request, out IActionResult? result)
        {
            request.Cookies.TryGetValue("access", out string? accessValue);

            if (accessValue == null || accessValue == "0")
            {
                result = new StatusCodeResult(403);
                return true;
            }

            result = null;
            return false;
        }
    }
}

using System;
using System.Linq;
using System.Security.Claims;

namespace WebApplication.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserID(this ClaimsPrincipal principal)
        {
            var claim = principal.Claims.FirstOrDefault(item => item.Type == ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new InvalidOperationException("No claim for the user's ID in the current claims.");
            
            if (string.IsNullOrEmpty(claim.Value))
                throw new InvalidOperationException("Couldn't determine user ID from claims!");

            if (claim.ValueType != ClaimValueTypes.Integer)
                throw new InvalidCastException("Cannot cast user ID claim to an integer.");

            return int.Parse(claim.Value);
        }

        public static string GetUsername(this ClaimsPrincipal principal)
        {
            var claim = principal.Claims.FirstOrDefault(item => item.Type == ClaimTypes.Name);
            
            if(claim == null)
                throw new InvalidOperationException("No claim for the user's username in the current claims.");

            return claim.Value;
        }

        public static string GetEmail(this ClaimsPrincipal principal)
        {
            var claim = principal.Claims.FirstOrDefault(item => item.Type == ClaimTypes.Email);
            
            if(claim == null)
                throw new InvalidOperationException("No claim for the user's email address in the current claims.");

            return claim.Value;
        }
    }
}

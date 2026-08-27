using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Stim.Api.Extensions;

public static class IdentityResultExtensions
{
    public static void AddToModelState(this IdentityResult result, ModelStateDictionary modelState)
    {
        foreach (var error in result.Errors)
        {
            var description = error.Code switch
            {
                "DuplicateUserName" => "Username is already taken.",
                "DuplicateEmail" => "Email is already in use.",
                _ => error.Description
            };

            modelState.AddModelError(error.Code, description);
        }
    }
}

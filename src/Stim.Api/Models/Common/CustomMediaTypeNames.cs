using System;

namespace Stim.Api.Models.Common;

public class CustomMediaTypeNames
{
    public static class Application
    {
        public const string Json = "application/json";

        public const string HateoasJson =
            "application/vnd.stim.hateoas+json";

        public const string HateoasJsonV2 =
            "application/vnd.stim.hateoas.2+json";
    }
}

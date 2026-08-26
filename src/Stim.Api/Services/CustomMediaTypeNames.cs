using System;

namespace Stim.Api.Services;

public class CustomMediaTypeNames
{
    public static class Application
    {
        public const string HateoasJsonMediaType = "application/vnd.stim.hateoas+json";
        public const string HateoasJsonMediaTypeV1 = "application/vnd.stim.hateoas.1.0+json";
        public const string HateoasJsonMediaTypeV2 = "application/vnd.stim.hateoas.2.0+json";
        public const string JsonV1 = "application/json;v=1.0";
        public const string JsonV2 = "application/json;v=2.0";
    }
}

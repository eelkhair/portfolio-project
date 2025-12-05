using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Routing.Controllers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JobBoard.API.Infrastructure.OpenApi;

/// <summary>
/// Converts route templates to kebab-case.
/// </summary>
public partial class KebabCaseRoutingConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {

            if (controller == null) continue;
         
            foreach (var action in controller.Actions)
            {

                if (action.Selectors == null) continue;
                foreach (var selector in action.Selectors)
                {
                    if (selector.AttributeRouteModel?.Template != null)
                    {
                        selector.AttributeRouteModel.Template =
                            TransformTemplate(selector.AttributeRouteModel.Template);
                    }
                }
            }
        }
    }

    private static string TransformTemplate(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        var parts = template.Split('/');
        for (var i = 0; i < parts.Length; i++)
        {
            if (!parts[i].Contains('{') && !parts[i].Contains('}'))
            {
                parts[i] = ToKebabCase(parts[i]);
            }
        }
        return string.Join('/', parts);
    }

    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input) || input.StartsWith('[') && input.EndsWith(']'))
        {
            return input;
        }

        return MyRegex().Replace(input, "-$1").ToLower();
    }
    
    public partial class KebabCaseParameterTransformer : IOutboundParameterTransformer
    {
        public string? TransformOutbound(object? value)
        {
            if (value is not string val || string.IsNullOrEmpty(val))
            {
                return null;
            } 
            
            return MyRegex().Replace(val, "-$1").ToLower();
        }

        [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.Compiled)]
        // ReSharper disable once MemberHidesStaticFromOuterClass
        private static partial Regex MyRegex();
    }

    [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
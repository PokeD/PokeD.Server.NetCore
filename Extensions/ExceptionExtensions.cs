using System;
using System.Linq;

namespace PokeD.Server.Windows.Extensions
{
    public static class ExceptionExtensions
    {
        public static string GetExceptionDetails(this Exception exception)
        {
            var properties = exception.GetType()
                .GetProperties();

            var fields = properties
                .Select(property => new { Name = property.Name, Value = property.GetValue(exception, null) })
                .Select(x => $"{x.Name} = {x.Value?.ToString() ?? string.Empty}");

            return string.Join("\n", fields);
        }
    }
}

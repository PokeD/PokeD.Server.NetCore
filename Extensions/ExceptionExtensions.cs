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
                .Select(x => string.Format("{0} = {1}", x.Name, x.Value != null ? x.Value.ToString() : string.Empty));

            return string.Join("\n", fields);
        }
    }
}

using System.Collections.Generic;

namespace ZZCityGen.WorldGenerator.Core.Validation
{
    public class ValidationResult
    {
        public bool IsValid = true;
        public List<string> Messages = new List<string>();
    }

    public interface IValidator<T>
    {
        ValidationResult Validate(T item);
    }

    public static class Validator
    {
        private static readonly List<object> validators = new List<object>();

        public static void Register<T>(IValidator<T> v) => validators.Add(v);

        public static ValidationResult Validate<T>(T item)
        {
            var result = new ValidationResult();
            foreach (var v in validators)
            {
                if (v is IValidator<T> typed)
                {
                    var r = typed.Validate(item);
                    if (!r.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.AddRange(r.Messages);
                    }
                }
            }
            return result;
        }
    }
}
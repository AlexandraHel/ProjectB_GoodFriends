using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppRazor.SeidoHelpers
{
    #region Seido Helpers for Model Validation
    public record ModelValidationResult
    (
        bool HasErrors,
        IEnumerable<string> ErrorMsgs,
        IEnumerable<KeyValuePair<string, ModelStateEntry>> InvalidKeys
    );

    public static class SeidoExtension
    {
        //Model state Validations
        public static bool IsValidPartially(this ModelStateDictionary model, out ModelValidationResult validationResult, string[] validateOnlyKeys = null)
        {
            var invalidKeys = model
               .Where(s => s.Value.ValidationState == ModelValidationState.Invalid);

            if (validateOnlyKeys != null)
            {
                invalidKeys = invalidKeys.Where(s => validateOnlyKeys.Any(vk => vk == s.Key));
            }

            var errorMsgs = invalidKeys.SelectMany(e => e.Value.Errors).Select(e => e.ErrorMessage);
            var hasErrors = invalidKeys.Count() != 0;

            validationResult = new ModelValidationResult(hasErrors, errorMsgs, invalidKeys);

            return !hasErrors;
        }


        //Populate SelectLists with Enum Values and Text
        public static List<SelectListItem> PopulateSelectList<TEnum>(this List<SelectListItem> selectList) where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("Not an enum type");
            }

            //Populate populate select tag with options using tag helpers
            foreach (var item in typeof(TEnum).GetEnumValues())
            {
                selectList.Add(new SelectListItem
                {
                    Value = item.ToString(),
                    Text = item.ToString()
                });
            }

            return selectList;
        }
    }
    #endregion
}


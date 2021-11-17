using System;
using FluentValidation;

namespace dcinc.api.queries
{
    public class WebMeetingsQueryParameterValidator : AbstractValidator<WebMeetingsQueryParameter>
    {
        public WebMeetingsQueryParameterValidator()
        {
            // Web会議の開始日と終了日が指定されている場合、終了日より未来日の開始日はNGとする。
            RuleFor(queryParameter => queryParameter.FromDateUtcValue).LessThanOrEqualTo(queryParameter => queryParameter.ToDateUtcValue).When(queryParameter => queryParameter.FromDate != null).WithMessage("fromDate is invalid. Please specify a date before toDate.");
            // Web会議の開始日と終了日が指定されている場合、開始日より過去日の終了日はNGとする。
            RuleFor(queryParameter => queryParameter.ToDateUtcValue).GreaterThanOrEqualTo(queryParameter => queryParameter.FromDateUtcValue).When(queryParameter => queryParameter.FromDate != null).WithMessage("toDate is invalid. Please specify a date after fromDate.");
        }
    }
}
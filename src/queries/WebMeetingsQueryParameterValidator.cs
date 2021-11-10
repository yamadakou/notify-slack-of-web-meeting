using System;
using FluentValidation;

namespace dcinc.api.queries
{
    public class WebMeetingsQueryParameterValidator : AbstractValidator<WebMeetingsQueryParameter>
    {
        public WebMeetingsQueryParameterValidator()
        {
            // Web会議の開始日と終了日が指定されている場合、終了日より未来日の開始日はNGとする。
            RuleFor(webMeeting => webMeeting.FromDate.Value).LessThanOrEqualTo(webMeeting => webMeeting.ToDate.Value).When(webMeeting => webMeeting.FromDate.HasValue && webMeeting.ToDate.HasValue).WithMessage("fromDate is invalid. Please specify a date before toDate.");
            // Web会議の開始日と終了日が指定されている場合、開始日より過去日の終了日はNGとする。
            RuleFor(webMeeting => webMeeting.ToDate.Value).GreaterThanOrEqualTo(webMeeting => webMeeting.FromDate.Value).When(webMeeting => webMeeting.FromDate.HasValue && webMeeting.ToDate.HasValue).WithMessage("toDate is invalid. Please specify a date after fromDate.");
        }
    }
}
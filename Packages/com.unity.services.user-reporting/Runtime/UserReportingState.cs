using System;

namespace Unity.Services.UserReporting
{
    enum UserReportingState
    {
        Idle = 0,
        CreatingUserReport = 1,
        ShowingForm = 2,
        SubmittingForm = 3
    }
}

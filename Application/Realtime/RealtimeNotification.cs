using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Realtime;

public sealed record RealtimeNotification(
    string EventName,
    Guid ProjectId,
    Guid? TaskId = null,
    Guid? CommentId = null);





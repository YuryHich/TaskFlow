using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Events;

public sealed record TagCatalogChangedEvent(): IAppEvent;
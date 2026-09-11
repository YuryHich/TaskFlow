using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IProjectAudience
{
   public  Task<IReadOnlyList<Guid>> GetUserIdsAsync(Guid projectId);
}

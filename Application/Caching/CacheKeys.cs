using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Caching;

public class CacheKeys
{
    public static string Project(Guid id) => $"project:{id}";
    public static string Task(Guid id) => $"task:{id}";
    public static string TagsAll = "tags:all";
}

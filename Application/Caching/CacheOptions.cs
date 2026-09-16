using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Caching;

public class CacheOptions
{
    public const string SectionName = "Cache";
    public int ProjectMinutes { get; set; } = 5;
    public int TaskMinutes { get; set; } = 2;
    public int TagsMinutes { get; set; } = 30;
}

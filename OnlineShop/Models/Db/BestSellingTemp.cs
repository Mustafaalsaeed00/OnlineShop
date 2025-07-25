using System;
using System.Collections.Generic;

namespace OnlineShop.Models.Db;

public partial class BestSellingTemp
{
    public int ProductId { get; set; }

    public int? ProductCount { get; set; }
}

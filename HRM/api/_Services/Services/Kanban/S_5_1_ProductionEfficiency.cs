using API._Repositories;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using API._Services.Interfaces.Kanban;
using API.Data;
using API.DTOs.Kanban;

namespace API._Services.Services.Kanban
{
    public class S_5_1_ProductionEfficiency : BaseServices, I_5_1_ProductionEfficiency
    {
        public S_5_1_ProductionEfficiency(DBContext dbContext) : base(dbContext)
        {
        }
        public async Task<List<ProductionEfficiencyDTO>> GetData(ProductionEfficiencyParam param)
        {
            // var result = await _repositoryAccessor.MS_Exception.FindAll(x => x.Shift == "1" 
            //         && x.mdat == param.ProductionDate 
            //         && x.uscod == param.Class)
            //     .Join(_repositoryAccessor.MS_Dispatch.FindAll(),
            //         x => new { x.Shift, x.mdat, x.uscod, x.bitnbr },
            //         y => new { y.Shift, y.mdat, y.uscod, y.bitnbr },
            //         (a, b) => new { a, b })
            //     .Join(_repositoryAccessor.MS_Category.FindAll(),
            //         x => x.a.uscod,
            //         y => y.ID,
            //         (x, y) => new { x.a, x.b, y })
            //     .GroupBy(x => new { x.a.Shift, x.a.mdat, x.c.NameEN, x.b.rmodel })
            //     .Select(g => new ProductionEfficiencyDTO
            //     {
            //         Shift = g.Key.Shift,
            //         Mdat = g.Key.mdat,
            //         NameEN = g.Key.NameEN,
            //         Rmodel = g.Key.rmodel,
            //         Qty = g.Sum(x => x.a.qty + x.a.nqty)
            //     })
            //     .OrderByDescending(x => x.Qty)
            //     .Take(5)
            //     .ToListAsync();
            var result = new List<ProductionEfficiencyDTO>
            {
                new ProductionEfficiencyDTO {
                    Category = "RB",
                    Detail = new ProductionEfficiencyDetail[]
                    {
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "RB", Rmodel = "00825", Qty = 120 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "RB", Rmodel = "1011C080", Qty = 110 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "RB", Rmodel = "42528", Qty = 100 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "RB", Rmodel = "69392", Qty = 90 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "RB", Rmodel = "05596", Qty = 80 }
                    }
                } ,
                new ProductionEfficiencyDTO {
                    Category = "CM",
                    Detail = new ProductionEfficiencyDetail[]
                    {
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "CM", Rmodel = "1011C080", Qty = 130 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "CM", Rmodel = "00825", Qty = 120 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "CM", Rmodel = "42528", Qty = 110 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "CM", Rmodel = "69392", Qty = 100 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "CM", Rmodel = "05596", Qty = 90 }
                    }
                }, 
                new ProductionEfficiencyDTO {
                    Category = "IP",
                    Detail = new ProductionEfficiencyDetail[]
                    {
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "IP", Rmodel = "42528", Qty = 140 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "IP", Rmodel = "1011C080", Qty = 130 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "IP", Rmodel = "00825", Qty = 120 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "IP", Rmodel = "69392", Qty = 110 },
                        new ProductionEfficiencyDetail { Shift = "1", Mdat = param.ProductionDate, NameEN = "IP", Rmodel = "05596", Qty = 100 }
                    }
                }
            };
            await Task.Delay(10);
            return result;
        }
    }
}
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BulkyBook.DataAccess.Repository
{
    public class OrderDetailsRepository : Repository<OrderDetails>, IOrderDetailsRepository
    {
        private readonly ApplicationDbContext _db; // as we have to update the database

        public OrderDetailsRepository(ApplicationDbContext db) : base(db) //dependency injection 
        {
            _db = db;
        }

        public void Update(OrderDetails obj) // we didn't include Update in main Repository.cs as update function variees from class to class
        {
            _db.Update(obj);
        }
    }
}

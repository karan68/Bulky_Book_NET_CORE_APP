using BulkyBook.DataAccess.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.DataAccess.Initilizer
{
  public interface IDbInitilizer
    {

        void Initialize();
    }
}

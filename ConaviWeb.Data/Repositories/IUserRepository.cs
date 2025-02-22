﻿using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConaviWeb.Data.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsers();
        Task<User> GetUserDetails(int id);
        Task<bool> InsertUser(CreateUser user);
        Task<bool> UpdateUser(User user);
        Task<bool> DeleteUser(User user);
    }
}

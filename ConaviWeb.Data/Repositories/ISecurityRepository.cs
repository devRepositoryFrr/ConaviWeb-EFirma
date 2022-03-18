using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using ConaviWeb.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConaviWeb.Data.Repositories
{
    public interface ISecurityRepository
    {
        Task<UserResponse> GetLoginByCredentials(UserRequest login);
        Task<UserResponse> GetLoginByUserId(int userId);
        Task<IEnumerable<Module>> GetModules(int idRol);
        Task<IEnumerable<Partition>> GetPartitions(int idSystem);
        Task<Partition> GetPartition(int idPartition);
    }
}

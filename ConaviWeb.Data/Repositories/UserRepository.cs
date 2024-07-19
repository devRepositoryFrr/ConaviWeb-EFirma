using Dapper;
using ConaviWeb.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConaviWeb.Model.Request;

namespace ConaviWeb.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MySQLConfiguration _connectionString;
        public UserRepository(MySQLConfiguration connectionString)
        {
            _connectionString = connectionString;
        }
        protected MySqlConnection DbConnection()
        {
            return new MySqlConnection(_connectionString.ConnectionString);
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id Id, nombre Name, primer_apellido LName, segundo_apellido SLName, usuario SUser, id_rol Rol, cargo Position,
                                numero_empleado EmployeeNumber, rfc RFC, grado_academico Degree, fecha_alta CreateDate, integrador Integrador, 
                                id_sistema IdSystem, firmante Signer, activo Active
                        FROM usuario;";

            return await db.QueryAsync<User>(sql, new { });
        }

        public async Task<User> GetUserDetails(int id)
        {
            var db = DbConnection();

            var sql = @"
                         SELECT id Id, nombre Name, primer_apellido LName, segundo_apellido SLName, usuario SUser, id_rol Rol, cargo Position,
                                numero_empleado EmployeeNumber, rfc RFC, grado_academico Degree, fecha_alta CreateDate, integrador Integrador, 
                                id_sistema IdSystem, area Area, firmante Signer, activo Active, posicion_firma PFirma, t_cargo_comite Cargo
                        FROM usuario
                        WHERE id = @Id";

            return await db.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<bool> InsertUser(CreateUser user)
        {
            var db = DbConnection();

            var sql = @"
                        INSERT INTO prod_web_efirma.usuario
                        (
                        nombre,
                        primer_apellido,
                        segundo_apellido,
                        usuario,
                        password,
                        id_rol,
                        rfc,
                        id_sistema,
                        email,
                        area)
                        VALUES
                        (
                        @Nombre,
                        @PApellido,
                        @SApellido,
                        @Usuario,
                        @Password,
                        @IdRol,
                        @RFC,
                        @IdSistema,
                        @Email,
                        @Dependencia
                        );
                        ";

            var result = await db.ExecuteAsync(sql, new {
                user.Nombre,
                user.PApellido,
                user.SApellido,
                user.Usuario,
                user.Password,
                user.IdRol,
                user.RFC,
                user.IdSistema,
                user.Email,
                user.Dependencia
            });
            return result > 0;
        }

        public async Task<bool> UpdateUser(User user)
        {
            var db = DbConnection();

            var sql = @"
                        ";

            var result = await db.ExecuteAsync(sql, new
            {
                
            });
            return result > 0;
        }

        public async Task<bool> DeleteUser(User user)
        {
            var db = DbConnection();

            var sql = @"
                        ";

            var result = await db.ExecuteAsync(sql, new { });
            return result > 0;
        }
    }
}

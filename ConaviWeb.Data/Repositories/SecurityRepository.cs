using Dapper;
using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using ConaviWeb.Model.Response;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConaviWeb.Data.Repositories
{
    public class SecurityRepository:ISecurityRepository
    {
        private readonly MySQLConfiguration _connectionString;
        public SecurityRepository(MySQLConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected MySqlConnection DbConnection()
        {
            return new MySqlConnection(_connectionString.ConnectionString);
        }
        public async Task<UserResponse> GetLoginByCredentials(UserRequest login)
        {
            try
            {

            
            var db = DbConnection();

            var sql = @"
                        SELECT id AS Id, concat(nombre,' ',primer_apellido,' ',segundo_apellido) Name, usuario AS SUser, id_rol AS Rol, id_sistema AS IdSistema
                        FROM usuario WHERE usuario = @SUser AND password = @Password AND activo = 1";

            return await db.QueryFirstOrDefaultAsync<UserResponse>(sql, new { login.SUser, login.Password });
            }
            catch (System.Exception e)
            {

                throw;
            }
        }
        public async Task<UserResponse> GetLoginByUserId(int userId)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id AS Id, usuario AS SUser, id_rol AS Rol
                        FROM usuario WHERE id = @UserId AND activo = 1";

            return await db.QueryFirstOrDefaultAsync<UserResponse>(sql, new { UserId = userId });
        }
        public async Task<IEnumerable<Module>> GetModules(int idRol)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id Id, descripcion Text, url Url, ico Ico
                        FROM rol_modulo rm
                        JOIN c_modulo cm ON rm.id_modulo = cm.id
                        where rm.id_rol = @IdRol";

            return await db.QueryAsync<Module>(sql, new { IdRol = idRol });
        }

        public async Task<IEnumerable<Partition>> GetPartitions(int idSystem)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id Id, descripcion Text
                        FROM c_particion
                        where id_sistema = @IdSystem
                        and estatus = 1";

            return await db.QueryAsync<Partition>(sql, new { IdSystem = idSystem });
        }

        public async Task<IEnumerable<Partition>> GetPartitionsBaja(int idSystem)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id Id, descripcion Text
                        FROM c_particion
                        where id_sistema = @IdSystem
                        ";

            return await db.QueryAsync<Partition>(sql, new { IdSystem = idSystem });
        }

        public async Task<IEnumerable<Partition>> GetPartitionsD(int idSystem, int idUser)
        {
            var db = DbConnection();

            var sql = @"
                        CALL sp_get_particion_descarga(@IdSystem, @IdUser);";

            return await db.QueryAsync<Partition>(sql, new { IdSystem = idSystem, IdUser = idUser});
        }

        public async Task<Partition> GetPartition(int idPartition)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id Id, descripcion Text, ruta_particion as PathPartition, json_proceso JsonUsers, nu_firmas Firmas
                        FROM c_particion
                        where id = @IdPartition
                        and estatus = 1";

            return await db.QueryFirstOrDefaultAsync<Partition>(sql, new { IdPartition = idPartition });
        }
        public async Task<IEnumerable<User>> GetUsers(int idSystem)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id Id, concat(concat_ws(' ', ifnull(nombre,''), ifnull(primer_apellido,''), ifnull(segundo_apellido,'')),' - ',t_cargo_comite) Name FROM prod_web_efirma.usuario
                        where id_sistema = @IdSystem and firmante = 1";

            return await db.QueryAsync<User>(sql, new { IdSystem = idSystem });
        }
        public async Task<Sistema> GetSystem(int idSystem)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT 
                        id Id, descripcion Descripcion, numero_firmas NumeroFirmas, activo Activo
                        FROM
                        prod_web_efirma.c_sistema
                        WHERE
                        id =@IdSystem";

            return await db.QueryFirstOrDefaultAsync<Sistema>(sql, new { IdSystem = idSystem });
        }

        public async Task<IEnumerable<Firmados>> GetFirmados(int idParticion)
        {
            var db = DbConnection();

            var sql = @"
                        call prod_web_efirma.sp_get_cantidad_firmados(@IdParticion);";

            return await db.QueryAsync<Firmados>(sql, new { IdParticion = idParticion });
        }
    }
}

using ConaviWeb.Model;
using ConaviWeb.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConaviWeb.Data.Repositories
{
    public interface IProcessSignRepository
    {
        Task<IEnumerable<FileResponse>> GetFiles(int idUser,int idSystem, int Estatus, int idRol, string rfc);
        Task<IEnumerable<FileResponse>> GetSignedFiles(int idSystem);
        Task<IEnumerable<FileResponse>> GetSignedFilesCancel(int idSystem);
        Task<IEnumerable<FileResponse>> GetPartitionFiles(int idPartition);
        Task<IEnumerable<FileResponse>> GetPartitionFilesCancel(int idPartition);
        Task<IEnumerable<FileResponse>> GetPartitionSourceFiles(int idPartition);
        Task<IEnumerable<FileResponse>> GetExternalFiles(string integrador);
        Task<IEnumerable<FileResponse>> GetFilesForSign(int idSystem, string arrayFiles, int estatus);
        Task<IEnumerable<FileResponse>> GetFilesForCancel(int idSystem, string arrayFiles);
        Task<bool> InsertSigningFile(SigningFile signingFile, User user, int idArchivoPadre, string currentXML, string XMLName, Partition partition);
        Task<EmailData> GetDataMail(int idArchivoPadre);
        Task<bool> InsertCancelFile(SigningFile signingFile, User user, int idArchivoPadre, string currentXML, string XMLName, Partition partition);
        Task<FileResponse> GetFileDownload(int idParticion);
        Task<IEnumerable<FileResponse>> GetFilesFirmados(int idParticion, int idEstatus, int idSistema);
        Task<EmailData> GetMailCarga(int idParticion);
        Task<bool> BajaArchivos(string ids, string obs);
        Task<bool> BajaParticion(string ids, string obs);
    }
}

﻿using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using ConaviWeb.Model.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConaviWeb.Services
{
    public interface IProcessSigningService
    {
        Task<Response> ProcessFileSatAsync(User user, DataSignRequest dataSignRequest, IEnumerable<FileResponse> files);
    }
}

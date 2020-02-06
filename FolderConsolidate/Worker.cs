using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MR.Config;

namespace FolderConsolidate
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servi�o iniciado: {time}", DateTimeOffset.Now);

            var sourceFolder = Config.Read("Consolidate:Source");
            var targetFolder = Config.Read("Consolidate:Target");
            var maskFiles = Config.Read("Consolidate:Mask");
            var DelayMS = 5000;

            _logger.LogInformation($"Source: {sourceFolder}");
            _logger.LogInformation($"Target: {targetFolder}");
            _logger.LogInformation($"Mask: {maskFiles}");

            try
            {
                DelayMS = Convert.ToInt32(Config.Read("Delay"));
            }
            catch (Exception)
            {
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Verificando arquivos...");

                var sourceFolderInfo = new DirectoryInfo(sourceFolder);
                var targetFolderInfo = new DirectoryInfo(targetFolder);
                if (!targetFolderInfo.Exists)
                {
                    _logger.LogWarning($"Diret�rio '{targetFolder}' n�o encontrado.");

                }
                else
                {
                    // DESCOBRIR TODOS OS ARQUIVOS DA ORIGEM
                    var files = sourceFolderInfo.GetFiles(maskFiles, SearchOption.AllDirectories).OrderBy(f => f.LastWriteTime).ToList();
                    foreach (var fileInfo in files)
                    {
                        // PARA CADA ARQUIVO, VERIFICAR O TAMANHO E COPIAR
                        if (fileInfo.Length > 0)
                        {
                            var splitFolder = fileInfo.DirectoryName.Split("\\");
                            var nextFileNumber = GetNumberOfNextFile(targetFolder);
                            var targetFileName =
                                targetFolderInfo.FullName + "\\" +
                                nextFileNumber.ToString("D3") + "_" +
                                splitFolder[splitFolder.Count() - 1] + "_" +
                                fileInfo.Name;

                            _logger.LogInformation($"Movendo {fileInfo.Name} para {targetFileName}...");

                            fileInfo.MoveTo(targetFileName);
                        }

                    }
                }
                await Task.Delay(DelayMS, stoppingToken);
            }
        }

        private int GetNumberOfNextFile(string target)
        {
            var files = Directory.GetFiles(target).OrderBy(o => o).ToList();
            var lastNumber = 0;

            if (files.Count() > 0)
            {
                // QUEBRA O NOME DO ARQUIVO EM "_" E PEGA A PRIMEIRA POSI��O (TEM QUE SER UM N�MERO)
                lastNumber = Convert.ToInt32(files[files.Count() - 1].Split("\\").Last().Split("_")[0]);
            }

            return (lastNumber + 1);
        }
    }
}

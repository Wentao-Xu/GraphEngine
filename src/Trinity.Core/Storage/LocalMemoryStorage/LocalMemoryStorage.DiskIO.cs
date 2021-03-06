// Graph Engine
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Trinity.Utilities;
using Trinity.Diagnostics;
using Trinity;
using Trinity.Daemon;
using System.Diagnostics;
using Trinity.Core.Lib;
using System.Runtime.CompilerServices;

namespace Trinity.Storage
{
    public unsafe partial class LocalMemoryStorage
    {
        /// <summary>
        /// Loads Trinity key-value store from disk to main memory.
        /// </summary>
        /// <returns>
        /// TrinityErrorCode.E_SUCCESS if loading succeeds; 
        /// Other error codes indicate a failure.
        /// </returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public TrinityErrorCode LoadStorage()
        {
            TrinityErrorCode ret = CLocalMemoryStorage.CLoadStorage();
            //TODO WAL and cell type signatures should migrate to KVStore extensions.
            InitializeWriteAheadLogFile();

            LoadCellTypeSignatures();

            try
            {
                StorageLoaded();
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Error, "StorageLoaded event handler: {0}", ex.ToString());
            }

            return ret;
        }

        /// <summary>
        /// Dumps the in-memory key-value store to disk files.
        /// </summary>
        /// <returns>
        /// TrinityErrorCode.E_SUCCESS if saving succeeds;
        /// Other error codes indicate a failure.
        /// </returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public TrinityErrorCode SaveStorage()
        {
            TrinityErrorCode ret = CLocalMemoryStorage.CSaveStorage();
            if (TrinityErrorCode.E_SUCCESS == ret)
            {
                CreateWriteAheadLogFile();

                SaveCellTypeSignatures();

                try
                {
                    StorageSaved();
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogLevel.Error, "StorageSaved event handler: {0}", ex.ToString());
                }
            }

            return ret;
        }

        /// <summary>
        /// Resets local memory storage to the initial state. The content in the memory storage will be cleared. And the memory storage will be shrunk to the initial size.
        /// </summary>
        /// <returns>
        /// TrinityErrorCode.E_SUCCESS if resetting succeeds; 
        /// Other error codes indicate a failure.
        /// </returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public TrinityErrorCode ResetStorage()
        {
            string path          = WriteAheadLogFilePath;
            TrinityErrorCode ret = CLocalMemoryStorage.CResetStorage();
            ResetWriteAheadLog(path);

            try
            {
                StorageReset();
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Error, "StorageReset event handler: {0}", ex.ToString());
            }

            return ret;
        }


        internal TrinityErrorCode GetTrinityImageSignature(TRINITY_IMAGE_SIGNATURE* pSignature)
        {
            return CLocalMemoryStorage.CGetTrinityImageSignature(pSignature);
        }

        #region Schema integrity check
        private void LoadCellTypeSignatures()
        {
            try
            {
                string path = Path.Combine(TrinityConfig.StorageRoot, c_celltype_signature_file_name);
                if (!File.Exists(path))
                    return;
                Log.WriteLine(LogLevel.Info, "Loading cell type signatures.");
                var schema_sig_from_storage_root = File.ReadAllLines(path);
                var schema_sig_from_tsl = Global.storage_schema.CellTypeSignatures.ToArray();

                if (schema_sig_from_storage_root.Length > schema_sig_from_tsl.Length)
                {
                    Log.WriteLine(LogLevel.Warning, "The disk image contains more cell types than defined in the loaded TSL storage extension!");
                }

                if (schema_sig_from_storage_root.Length < schema_sig_from_tsl.Length)
                {
                    Log.WriteLine(LogLevel.Warning, "The disk image contains less cell types than defined in the loaded TSL storage extension!");
                }

                int min_len = Math.Min(schema_sig_from_storage_root.Length, schema_sig_from_tsl.Length);

                for (int i=0; i<min_len; ++i)
                {
                    if (schema_sig_from_storage_root[i] != schema_sig_from_tsl[i])
                    {
                        Log.WriteLine(LogLevel.Error, "Inconsistent cell type signature for type #{0}.", i);
                        Log.WriteLine(LogLevel.Error, "Expecting: {0}.", schema_sig_from_tsl[i]);
                        Log.WriteLine(LogLevel.Error, "Got: {0}.", schema_sig_from_storage_root[i]);
                    }
                }
            }
            catch
            {
                Log.WriteLine(LogLevel.Error, "Errors occurred while examining storage schema signature.");
            }
        }

        private void SaveCellTypeSignatures()
        {
            Log.WriteLine(LogLevel.Info, "Saving cell type signatures.");
            try
            {
                File.WriteAllLines(Path.Combine(TrinityConfig.StorageRoot, c_celltype_signature_file_name), Global.storage_schema.CellTypeSignatures);
            }
            catch
            {
                Log.WriteLine(LogLevel.Error, "Errors occurred while saving storage schema signature.");
            }
        }
        #endregion
    }
}

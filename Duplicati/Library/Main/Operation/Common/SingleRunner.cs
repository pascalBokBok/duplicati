﻿//  Copyright (C) 2015, The Duplicati Team
//  http://www.duplicati.com, info@duplicati.com
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Threading.Tasks;
using CoCoL;

namespace Duplicati.Library.Main.Operation.Common
{
    internal abstract class SingleRunner : ProcessHelper
    {
        protected IChannel<Action> m_channel;

        public SingleRunner()
        {
            m_channel = ChannelManager.CreateChannel<Action>();
        }

        protected override async Task Start()
        {
            while(true)
                await m_channel.ReadAsync()(); // Grab-n-execute
        }

        protected async Task<T> RunOnMain<T>(Func<T> method)
        {
            var res = new TaskCompletionSource<T>();
            try
            {
                await m_channel.WriteAsync(() => {
                    try
                    {
                        res.SetResult(method());
                    }
                    catch(Exception ex)
                    {
                        if (ex is System.Threading.ThreadAbortException)
                            res.TrySetCanceled();
                        else
                            res.TrySetException(ex);
                    }
                }).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                if (ex is System.Threading.ThreadAbortException)
                    res.TrySetCanceled();
                else
                    res.TrySetException(ex);
            }

            return await res.Task;
        }

        protected async Task RunOnMain(Action method)
        {
            return RunOnMain<bool>(() =>
            {
                method();
                return true;
            });
        }

        #region IDisposable implementation

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        protected void Dispose(bool isDisposing)
        {
            if (m_channel != null)
                try { m_channel.Retire(); }
                catch { }
                finally { m_channel = null; } 
        }
    }
}

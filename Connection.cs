﻿/*
 * Created by SharpDevelop.
 * User: User
 * Date: 27.04.2008
 * Time: 16:23
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Threading;
using System.Collections.Generic;

namespace Pipeline1
{
	#region Exceptions
	public class ConnectionIdMustBeUnique : Exception {}
	public class CannotFindConnection : Exception {}
	#endregion
		
	#region Connection
	/// <summary>
	/// Base class for connections
	/// </summary>
	public class Connection
	{
		private int _available = 0;
		private Queue<int> _queue = new Queue<int>();
		private object _syncRoot = new object();
		
		public int Available
		{
			get {lock (_syncRoot){ return _available;}}
			protected set { lock(_syncRoot){_available = value;}}
		}
		
		public Connection Acquire()
		{
			lock (_syncRoot)
			{
				//TODO: check if ManagedThreadId is unique
				int id = Thread.CurrentThread.ManagedThreadId;
				
				if (_available > 0 && _queue.Count == 0)
				{
					_available--;
					return this;
				}
				else
					_queue.Enqueue(id);
				
				//wait until resource available and your thread is next in queue
				while (_available==0 || _queue.Peek() != id)
					Monitor.Wait(_syncRoot);
				
				_queue.Dequeue();
				_available--;
				
				Monitor.PulseAll(_syncRoot);				
			}
			return this;			
		}
		
		public void Release()
		{
			lock(_syncRoot)
			{
				_available++;
				Monitor.PulseAll(_syncRoot);
			}
		}
	}
	#endregion
	
	#region ConnectionManager
//	public class ConnectionManager
//	{
//		private static volatile ConnectionManager _instance;
//		private static object _syncRoot = new Object();
//		public Dictionary<string, Connection> _connections = new Dictionary<string, Connection>();
//		
//		private ConnectionManager() {}
//		
//		public static ConnectionManager Instance
//		{
//		  get 
//		  {
//		     if (_instance == null) 
//		     {
//		        lock (_syncRoot) 
//		        {
//		           if (_instance == null) 
//		              _instance = new ConnectionManager();
//		        }
//		     }
//		
//		     return _instance;
//		  }
//		}
//		
//		public void Add(string id, Connection connection)
//		{
//			lock(_syncRoot)
//			{
//				if (_connections.ContainsKey(id))
//					throw new ConnectionIdMustBeUnique();				
//				_connections.Add(id,connection);
//			}
//		}
//		
//		public Connection Connection(string id)
//		{
//			lock(_syncRoot)
//			{
//				if (!_connections.ContainsKey(id))
//					throw new CannotFindConnection();
//				return _connections[id];
//			}
//		}
//	}
	
	public sealed class ConnectionManager
	{
		private static volatile ConnectionManager _instance;
		private static object _syncRoot = new Object();
		private static Dictionary<string, Connection> _connections = new Dictionary<string, Connection>();
		
		private ConnectionManager() {}
		
		public static ConnectionManager Instance
		{
		  get 
		  {
		     if (_instance == null) 
		     {
		        lock (_syncRoot) 
		        {
		           if (_instance == null) 
		              _instance = new ConnectionManager();
		        }
		     }
		
		     return _instance;
		  }
		}
		
		public static void Add(string id, Connection connection)
		{
			lock(_syncRoot)
			{
				if (_connections.ContainsKey(id))
					throw new ConnectionIdMustBeUnique();				
				_connections.Add(id,connection);
			}
		}
		
		public static Connection Connection(string id)
		{
			lock(_syncRoot)
			{
				if (!_connections.ContainsKey(id))
					throw new CannotFindConnection();
				return _connections[id];
			}
		}
	}
	
	#endregion
	
	#region FileConnection
	public class FileConnection : Connection
	{
		public readonly string Path;
		
		public FileConnection(string path)
		{
			Path = path;
			Available = 1;
		}
	}
	#endregion
}

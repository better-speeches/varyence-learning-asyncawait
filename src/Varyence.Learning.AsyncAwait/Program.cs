using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Varyence.Learning.AsyncAwait
{
    public class Program
    {
        private static async Task Main()
        {
            //Console.WriteLine(await MyAwaitableMethod());
            await TestAsyncYielding();
            //AsyncVoidExceptions_CannotBeCaughtByCatch();
            
            Console.ReadLine();
        }

        #region custom awaitable

        private static async MyAwaitable<int> MyAwaitableMethod()
        {
            int result = 0;
            int arg1 = await GetMyAwaitable(1);
            result += arg1;
            int arg2 = await GetMyAwaitable(2);
            result += arg2;
            int arg3 = await GetMyAwaitable(3);
            result += arg3;
            return result;
        }

        private static async MyAwaitable<int> GetMyAwaitable(int arg)
        {
            await Task.Delay(1); //Simulate asynchronous execution 
            return await new MyAwaitable<int>(arg);
        }

        #endregion

        #region async void
        
        private static async void ThrowExceptionAsync()
        {
            throw new InvalidOperationException();
        }
        public static void AsyncVoidExceptions_CannotBeCaughtByCatch()
        {
            try
            {
                ThrowExceptionAsync();
            }
            catch (Exception)
            {
                // The exception is never caught here!
                throw;
            }
        }

        #endregion
        
        #region IAsyncEnumerable<T>
        
        private static async Task TestAsyncYielding()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            IAsyncEnumerable<int> enumerable = AsyncYielding();
            Console.WriteLine($"Time after calling: {sw.ElapsedMilliseconds}");

            await foreach (var element in enumerable.ConfigureAwait(false))
            {
                Console.WriteLine($"element: {element}");
                Console.WriteLine($"Time: {sw.ElapsedMilliseconds}");
            }
        }
        
        private static async IAsyncEnumerable<int> AsyncYielding()
        {
            foreach (var uselessElement in Enumerable.Range(1, 3))
            {
                var task = Task.Delay(TimeSpan.FromSeconds(uselessElement));
                Console.WriteLine($"Task run: {uselessElement}");
                await task;
                yield return uselessElement;
            }
        }

        #endregion
    }

    #region custom awaitable

    [AsyncMethodBuilder(typeof(MyAwaitableTaskMethodBuilder<>))]
    public class MyAwaitable<T> : INotifyCompletion
    {
        private Action _continuation;
    
        public MyAwaitable()
        { }
    
        public MyAwaitable(T value)
        {
            Value = value;
            IsCompleted = true;
        }
    
        public MyAwaitable<T> GetAwaiter() => this;
    
        public bool IsCompleted { get; private set; }
    
        public T Value { get; private set; }
    
        public Exception Exception { get; private set; }
    
        public T GetResult()
        {
            if (!IsCompleted) throw new Exception("Not completed");
            if (Exception != null)
            {
                ExceptionDispatchInfo.Throw(Exception);
            }
            return Value;
        }
    
        internal void SetResult(T value)
        {
            if (IsCompleted) throw new Exception("Already completed");
            Value = value;
            IsCompleted = true;
            _continuation?.Invoke();
        }
    
        internal void SetException(Exception exception)
        {
            IsCompleted = true;
            Exception = exception;
        }
    
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            _continuation = continuation;
            if (IsCompleted)
            {
                continuation();
            }
        }
    }
    
    public class MyAwaitableTaskMethodBuilder<T>
    {
        public MyAwaitableTaskMethodBuilder() 
            => Task = new MyAwaitable<T>();
    
        public static MyAwaitableTaskMethodBuilder<T> Create() 
        => new MyAwaitableTaskMethodBuilder<T>();
    
        public void Start<TStateMachine>(ref TStateMachine stateMachine) 
            where TStateMachine : IAsyncStateMachine 
            => stateMachine.MoveNext();
    
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    
        public void SetException(Exception exception) 
            => Task.SetException(exception);
    
        public void SetResult(T result) 
            => Task.SetResult(result);
    
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, 
            ref TStateMachine stateMachine) 
            where TAwaiter : INotifyCompletion 
            where TStateMachine : IAsyncStateMachine
            => GenericAwaitOnCompleted(ref awaiter, ref stateMachine);
    
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, 
            ref TStateMachine stateMachine) 
            where TAwaiter : ICriticalNotifyCompletion 
            where TStateMachine : IAsyncStateMachine
            => GenericAwaitOnCompleted(ref awaiter, ref stateMachine);
    
        public void GenericAwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, 
            ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine         
            => awaiter.OnCompleted(stateMachine.MoveNext);
    
        public MyAwaitable<T> Task { get; }
    }

    #endregion
}
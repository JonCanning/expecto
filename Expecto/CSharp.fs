namespace Expecto.CSharp

#nowarn "46"

open System
open System.Threading.Tasks
open System.Runtime.CompilerServices
open Expecto
open Expecto.Impl

// C# is weak and can't really handle Async or partial application
type ITestPrinter =
  abstract member BeforeRun   : Test -> Task
  abstract member BeforeEach  : string -> Task
  abstract member Info        : string -> Task
  abstract member Passed      : string * TimeSpan -> Task
  abstract member Ignored     : string * string -> Task
  abstract member Failed      : string * string * TimeSpan -> Task
  abstract member Exn         : string * exn * TimeSpan -> Task
  abstract member Summary     : ExpectoConfig * TestRunSummary -> Task


[<AutoOpen; Extension>]
module Runner =
  open System.Collections.Generic

  let private printerFromInterface (i : ITestPrinter) =
      { beforeRun   = fun t ->      async { return! i.BeforeRun(t) |> Async.AwaitTask }
        beforeEach  = fun s ->      async { return! i.BeforeEach(s) |> Async.AwaitTask }
        passed      = fun n d ->    async { return! i.Passed(n, d) |> Async.AwaitTask }
        info        = fun s ->      async { return! i.Info(s) |> Async.AwaitTask }
        ignored     = fun n m ->    async { return! i.Ignored(n, m) |> Async.AwaitTask }
        failed      = fun n m d ->  async { return! i.Failed(n, m, d) |> Async.AwaitTask }
        exn         = fun n e d ->  async { return! i.Exn(n, e, d) |> Async.AwaitTask }
        summary     = fun c s ->    async { return! i.Summary(c, s) |> Async.AwaitTask }
      }

  type Async with
    static member AwaitTask(t : Task) =
      let tcs = TaskCompletionSource<unit>()
      t.ContinueWith(fun t ->
        if t.IsCompleted then tcs.SetResult(())
        else if t.IsFaulted then tcs.SetException(t.Exception)
        else if t.IsCanceled then tcs.SetCanceled()
      ) |> ignore
      Async.AwaitTask tcs.Task

  let RunTests(config, tests) = runEval config tests
  let RunTestsWithArgs(config, args, tests) = runTestsWithArgs config args tests
  let RunTestsInAssembly(config, args) = runTestsInAssembly config args
  let ListTests(tests) = listTests tests
  let TestList(name, tests: IEnumerable<Test>) = testList name (List.ofSeq tests)
  [<CompiledName("TestCase")>]
  let TestCaseA(name, test: System.Action) = testCase name test.Invoke
  [<CompiledName("TestCase")>]
  let TestCaseT(name, test: Task) = testCaseAsync name (Async.AwaitTask test)
  [<CompiledName("TestCase")>]
  let TestCaseFT(name, test: System.Func<Task>) = testCaseAsync name (async { do! Async.AwaitTask (test.Invoke()) })
  [<CompiledName("PendingTestCase")>]
  let PendingTestCaseA(name, test: System.Action) = ptestCase name test.Invoke
  [<CompiledName("PendingTestCase")>]
  let PendingTestCaseT(name, test: Task) = ptestCaseAsync name (Async.AwaitTask test)
  [<CompiledName("PendingTestCase")>]
  let PendingTestCaseFT(name, test: System.Func<Task>) = ptestCaseAsync name (async { do! Async.AwaitTask (test.Invoke()) })
  [<CompiledName("FocusedTestCase")>]
  let FocusedTestCaseA(name, test: System.Action) = ftestCase name test.Invoke
  [<CompiledName("FocusedTestCase")>]
  let FocusedTestCaseT(name, test: Task) = ftestCaseAsync name (Async.AwaitTask test)
  [<CompiledName("FocusedTestCase")>]
  let FocusedTestCaseFT(name, test: System.Func<Task>) = ftestCaseAsync name (async { do! Async.AwaitTask (test.Invoke()) })
  let DefaultConfig = defaultConfig

  type Expecto.Impl.ExpectoConfig with

      // C# is weak and it is hard to update the configuration
      [<Extension; CompiledName("WithParallel")>]
      member x.WithParallel(parallel) = { x with parallel = parallel }

      [<Extension; CompiledName("WithParallelWorkers")>]
      member x.WithParallelWorkers(parallelWorkers) = { x with parallelWorkers = parallelWorkers }

      [<Extension; CompiledName("WithStress")>]
      member x.WithStress(stress) = { x with stress = stress }

      [<Extension; CompiledName("WithStressTimeout")>]
      member x.WithStressTimeout(stressTimeout) = { x with stressTimeout = stressTimeout }

      [<Extension; CompiledName("WithStressMemoryLimit")>]
      member x.WithStressMemoryLimit(stressMemoryLimit) = { x with stressMemoryLimit = stressMemoryLimit }

      [<Extension; CompiledName("WithFilter")>]
      member x.WithFilter(filter) = { x with filter = filter }

      [<Extension; CompiledName("WithFailOnFocusedTests")>]
      member x.WithFailOnFocusedTests(failOnFocusedTests) = { x with failOnFocusedTests = failOnFocusedTests }

      [<Extension; CompiledName("WithPrinter")>]
      member x.WithPrinter(printer) = { x with printer = printer }

      /// Set the printer - this replaces the current one
      [<Extension; CompiledName("WithPrinter")>]
      member x.WithPrinter(printer:ITestPrinter) = { x with printer = printerFromInterface printer }

      /// Add an additional printer - this does not replace the current one
      [<Extension; CompiledName("AddPrinter")>]
      member x.AddPrinter(printer:ITestPrinter) =
        let combinedPrinter = TestPrinters.mergePrinters (x.printer, (printerFromInterface printer))
        { x with printer = combinedPrinter }

      [<Extension; CompiledName("WithVerbosity")>]
      member x.WithVerbosity(verbosity) = { x with verbosity = verbosity }

      [<Extension; CompiledName("WithLogName")>]
      member x.WithLogName(logName) = { x with logName = logName }

      [<Extension; CompiledName("WithLocate")>]
      member x.WithLocate(locate) = { x with locate = locate }

      [<Extension; CompiledName("WithFsCheckMaxTests")>]
      member x.WithFsCheckMaxTests(fsCheckMaxTests) = { x with fsCheckMaxTests = fsCheckMaxTests }

      [<Extension; CompiledName("WithFsCheckStartSize")>]
      member x.WithFsCheckStartSize(fsCheckStartSize) = { x with fsCheckStartSize = fsCheckStartSize }

      [<Extension; CompiledName("WithFsCheckEndSize")>]
      member x.WithFsCheckEndSize(fsCheckEndSize) = { x with fsCheckEndSize = fsCheckEndSize }

      [<Extension; CompiledName("WithMySpiritIsWeak")>]
      member x.WithMySpiritIsWeak(mySpiritIsWeak) = { x with mySpiritIsWeak = mySpiritIsWeak }

      [<Extension; CompiledName("WithAllowDuplicateNames")>]
      member x.WithAllowDuplicateNames(allowDuplicateNames) = { x with allowDuplicateNames = allowDuplicateNames }
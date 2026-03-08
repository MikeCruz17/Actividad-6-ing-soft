using System.Collections.Concurrent;
using System.Diagnostics;

namespace STRFloodAlert
{
    // =========================
    //  STR INUNDACIONES (TU BASE)
    // =========================
    enum RiskState { Normal, Vigilancia, Alerta, Emergencia }

    record SensorReading(
        string SensorId,
        string Zone,
        double WaterLevelM,
        double RainMmH,
        DateTime SensorTimeUtc
    );

    // =========================
    //  STR SEMÁFORO (ACTIVIDAD)
    // =========================
    enum LightState { Verde, Amarillo, Rojo, Intermitente }

    class Program
    {
        // -------------------------
        // Deadlines Inundaciones
        // -------------------------
        static readonly TimeSpan DL_Ingest = TimeSpan.FromMilliseconds(200);

        // Umbrales (ejemplo)
        const double TH_Water_Vigilancia = 2.0;
        const double TH_Water_Alerta = 3.0;
        const double TH_Water_Emerg = 3.5;

        const double TH_Rain_Vigilancia = 30;
        const double TH_Rain_Alerta = 60;
        const double TH_Rain_Emerg = 90;

        static readonly BlockingCollection<SensorReading> IngestQueue = new(1000);

        static volatile int SimulationMode = 1; // 1=Normal,2=Vigilancia,3=Alerta,4=Emergencia

        // -------------------------
        // Deadlines Semáforo
        // -------------------------
        static readonly TimeSpan TL_TICK = TimeSpan.FromMilliseconds(200);
        static readonly TimeSpan TL_DL_TICK = TimeSpan.FromMilliseconds(120);

        static readonly TimeSpan TL_VERDE = TimeSpan.FromSeconds(10);
        static readonly TimeSpan TL_AMARILLO = TimeSpan.FromSeconds(3);
        static readonly TimeSpan TL_ROJO = TimeSpan.FromSeconds(10);

        static async Task Main()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                while (true)
                {
                    try
                    {
                        Console.WriteLine("\n=== PROYECTO STR (C# Consola) ===");
                        Console.WriteLine("[1] STR Inundaciones (Simulación)");
                        Console.WriteLine("[2] STR Semáforo (Actividad)");
                        Console.WriteLine("[q] Salir");
                        Console.Write("Seleccione: ");

                        var key = Console.ReadKey(true).KeyChar;
                        Console.WriteLine();

                        if (key == 'q') break;
                        if (key == '1') await RunFloodAlertAsync();
                        else if (key == '2') await RunTrafficLightAsync();
                        else Console.WriteLine("Opción inválida.");
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine($"[ERROR] Problema de entrada/salida: {ioEx.Message}");
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("[INFO] Operación cancelada por el usuario.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Excepción inesperada en menú principal: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR CRÍTICO] Fallo fatal en Main: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("\n[INFO] Aplicación finalizada.");
            }
        }

        // =========================================================
        //  MÓDULO 1: STR INUNDACIONES (TU CÓDIGO, ORDENADO)
        // =========================================================
        static async Task RunFloodAlertAsync()
        {
            try
            {
                Console.WriteLine("=== STR: Alerta Temprana Inundaciones (Simulación) ===");
                Console.WriteLine("Comandos: [1]=Normal  [2]=Vigilancia  [3]=Alerta  [4]=Emergencia  [q]=Volver al menú");
                Console.WriteLine();

                // Reset cola por si vuelves a entrar
                try
                {
                    while (IngestQueue.TryTake(out _)) { }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ADVERTENCIA] Error al limpiar la cola: {ex.Message}");
                }

                using var cts = new CancellationTokenSource();

                var ingestTask = Task.Run(() => IngestLoop(cts.Token));
                var generatorTask = Task.Run(() => SensorGeneratorLoop(cts.Token));

                while (true)
                {
                    try
                    {
                        var key = Console.ReadKey(true).KeyChar;
                        if (key == 'q') break;

                        if (key is '1' or '2' or '3' or '4')
                        {
                            SimulationMode = key switch
                            {
                                '1' => 1,
                                '2' => 2,
                                '3' => 3,
                                '4' => 4,
                                _ => 1
                            };
                            Console.WriteLine($"[UI] Modo: {ModeName(SimulationMode)}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] En lectura de entrada: {ex.Message}");
                    }
                }

                try
                {
                    cts.Cancel();
                    IngestQueue.CompleteAdding();

                    await Task.WhenAll(ingestTask, generatorTask);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[INFO] Tareas canceladas correctamente.");
                }
                catch (AggregateException aggEx)
                {
                    Console.WriteLine($"[ERROR] Error en tareas paralelas: {aggEx.InnerException?.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] En finalización de tareas: {ex.Message}");
                }

                Console.WriteLine("Volviendo al menú...\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR CRÍTICO] En RunFloodAlertAsync: {ex.Message}");
            }
        }

        static string ModeName(int mode) => mode switch
        {
            1 => "Normal",
            2 => "Vigilancia",
            3 => "Alerta",
            4 => "Emergencia",
            _ => "Normal"
        };

        static void SensorGeneratorLoop(CancellationToken ct)
        {
            try
            {
                var rnd = new Random();
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        var zone = "Zona-Norte";
                        var sensorId = "SEN-001";

                        (double wl, double rain) = SimulationMode switch
                        {
                            1 => (1.2 + rnd.NextDouble() * 0.3, 5 + rnd.NextDouble() * 5),
                            2 => (2.1 + rnd.NextDouble() * 0.3, 35 + rnd.NextDouble() * 10),
                            3 => (3.1 + rnd.NextDouble() * 0.3, 70 + rnd.NextDouble() * 10),
                            4 => (3.6 + rnd.NextDouble() * 0.4, 95 + rnd.NextDouble() * 15),
                            _ => (1.2, 5)
                        };

                        if (rnd.Next(0, 20) == 0) wl = -1; // inválido a veces

                        var reading = new SensorReading(sensorId, zone, wl, rain, now);

                        try
                        {
                            IngestQueue.Add(reading, ct);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (InvalidOperationException ex)
                        {
                            Console.WriteLine($"[ERROR] La cola se completó: {ex.Message}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Al agregar dato a la cola: {ex.Message}");
                        }

                        try
                        {
                            Thread.Sleep(250);
                        }
                        catch (ThreadInterruptedException)
                        {
                            Console.WriteLine("[INFO] GeneradorSensor interrumpido.");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] En iteración del generador: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR CRÍTICO] En SensorGeneratorLoop: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("[INFO] Generador de sensores finalizado.");
            }
        }

        static void IngestLoop(CancellationToken ct)
        {
            try
            {
                foreach (var reading in IngestQueue.GetConsumingEnumerable(ct))
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();

                        var serverTime = DateTime.UtcNow;

                        // VALIDACIÓN + EVALUACIÓN + ALERTA (simple, pero completa)
                        var validationOk = ValidateReading(reading, out var validationMsg);
                        var risk = RiskState.Normal;
                        var rule = "N/A";

                        if (validationOk)
                        {
                            try
                            {
                                (risk, rule) = EvaluateRisk(reading);
                                if (risk is RiskState.Alerta or RiskState.Emergencia)
                                    EmitAlert(reading.Zone, risk, rule);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] En evaluación de riesgo: {ex.Message}");
                                risk = RiskState.Normal;
                                rule = "ERROR";
                            }
                        }

                        sw.Stop();
                        try
                        {
                            LogDeadline("INGESTA+PROC", sw.Elapsed, DL_Ingest,
                                $"WL={reading.WaterLevelM:F2}m Rain={reading.RainMmH:F1}mm/h | Valid={validationOk} {validationMsg} | Estado={risk} Regla={rule} | tS={reading.SensorTimeUtc:HH:mm:ss.fff}Z tSrv={serverTime:HH:mm:ss.fff}Z");
                        }
                        catch (FormatException fEx)
                        {
                            Console.WriteLine($"[ERROR] En formateo del log: {fEx.Message}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Procesando lectura: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[INFO] IngestLoop cancelado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR CRÍTICO] En IngestLoop: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("[INFO] Loop de ingesta finalizado.");
            }
        }

        static bool ValidateReading(SensorReading r, out string msg)
        {
            msg = "";
            try
            {
                if (r == null)
                {
                    msg = "(lectura nula)";
                    return false;
                }

                if (r.WaterLevelM < 0 || r.WaterLevelM > 10)
                {
                    msg = "(dato inválido WL)";
                    return false;
                }
                if (r.RainMmH < 0 || r.RainMmH > 300)
                {
                    msg = "(dato inválido lluvia)";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                msg = $"(error en validación: {ex.Message})";
                return false;
            }
        }

        static (RiskState state, string rule) EvaluateRisk(SensorReading r)
        {
            try
            {
                if (r == null)
                    return (RiskState.Normal, "Lectura nula");

                // Regla simple por máximos
                if (r.WaterLevelM >= TH_Water_Emerg || r.RainMmH >= TH_Rain_Emerg)
                    return (RiskState.Emergencia, "WL>=3.5 o Rain>=90");
                if (r.WaterLevelM >= TH_Water_Alerta || r.RainMmH >= TH_Rain_Alerta)
                    return (RiskState.Alerta, "WL>=3.0 o Rain>=60");
                if (r.WaterLevelM >= TH_Water_Vigilancia || r.RainMmH >= TH_Rain_Vigilancia)
                    return (RiskState.Vigilancia, "WL>=2.0 o Rain>=30");

                return (RiskState.Normal, "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] En EvaluateRisk: {ex.Message}");
                return (RiskState.Normal, "ERROR");
            }
        }

        static void EmitAlert(string zone, RiskState state, string rule)
        {
            try
            {
                if (string.IsNullOrEmpty(zone))
                    zone = "ZONA-DESCONOCIDA";

                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}Z] [ALERTA] Zona={zone} Estado={state} Regla={rule}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] En EmitAlert: {ex.Message}");
            }
        }

        // =========================================================
        //  MÓDULO 2: STR SEMÁFORO (ACTIVIDAD)
        // =========================================================
        static async Task RunTrafficLightAsync()
        {
            try
            {
                Console.WriteLine("=== STR: Semáforo (Actividad) ===");
                Console.WriteLine("Controles: [n]=modo noche(intermitente)  [a]=auto normal  [q]=Volver al menú\n");

                using var cts = new CancellationTokenSource();

                var nightMode = false;
                var state = LightState.Rojo;
                var stateEnds = DateTime.UtcNow + TL_ROJO;

                void Enter(LightState s)
                {
                    try
                    {
                        state = s;
                        var dur = s switch
                        {
                            LightState.Verde => TL_VERDE,
                            LightState.Amarillo => TL_AMARILLO,
                            LightState.Rojo => TL_ROJO,
                            LightState.Intermitente => TimeSpan.MaxValue,
                            _ => TL_ROJO
                        };

                        stateEnds = (dur == TimeSpan.MaxValue) ? DateTime.MaxValue : DateTime.UtcNow + dur;
                        Console.WriteLine($"{Stamp()} [CAMBIO] -> {state} (duración: {(dur == TimeSpan.MaxValue ? "∞" : dur.TotalSeconds + "s")})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] En Enter: {ex.Message}");
                    }
                }

                // tarea de input para no bloquear el tick
                var inputTask = Task.Run(() =>
                {
                    try
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            try
                            {
                                var k = Console.ReadKey(true).KeyChar;
                                if (k == 'q') { cts.Cancel(); break; }
                                if (k == 'n')
                                {
                                    nightMode = true;
                                    Enter(LightState.Intermitente);
                                    Console.WriteLine($"{Stamp()} [MODO] NOCHE/INTERMITENTE");
                                }
                                if (k == 'a')
                                {
                                    nightMode = false;
                                    Enter(LightState.Rojo);
                                    Console.WriteLine($"{Stamp()} [MODO] AUTO NORMAL");
                                }
                            }
                            catch (IOException ioEx)
                            {
                                Console.WriteLine($"[ERROR] Entrada/salida: {ioEx.Message}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] En lectura de input: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR CRÍTICO] En inputTask: {ex.Message}");
                    }
                });

                // tarea periódica (tick) -> esencia STR
                var tickTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            try
                            {
                                var sw = Stopwatch.StartNew();

                                if (nightMode)
                                {
                                    Console.WriteLine($"{Stamp()} [INTERMITENTE] Amarillo parpadeando...");
                                }
                                else
                                {
                                    try
                                    {
                                        var now = DateTime.UtcNow;
                                        if (now >= stateEnds)
                                        {
                                            var next = state switch
                                            {
                                                LightState.Verde => LightState.Amarillo,
                                                LightState.Amarillo => LightState.Rojo,
                                                LightState.Rojo => LightState.Verde,
                                                _ => LightState.Rojo
                                            };
                                            Enter(next);
                                        }
                                        else
                                        {
                                            var rem = stateEnds - now;
                                            Console.WriteLine($"{Stamp()} [ESTADO] {state,-8} | restante: {rem.TotalSeconds,5:0.0}s");
                                        }
                                    }
                                    catch (OverflowException ovEx)
                                    {
                                        Console.WriteLine($"[ERROR] Desbordamiento en cálculo de tiempo: {ovEx.Message}");
                                    }
                                }

                                sw.Stop();
                                try
                                {
                                    LogDeadline("TICK", sw.Elapsed, TL_DL_TICK, "Control periódico del semáforo");
                                }
                                catch (FormatException fEx)
                                {
                                    Console.WriteLine($"[ERROR] En LogDeadline: {fEx.Message}");
                                }

                                try
                                {
                                    await Task.Delay(TL_TICK, cts.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] En iteración tick: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR CRÍTICO] En tickTask: {ex.Message}");
                    }
                });

                Enter(LightState.Rojo);

                try
                {
                    await Task.WhenAll(tickTask, inputTask);
                }
                catch (AggregateException aggEx)
                {
                    Console.WriteLine($"[ERROR] En tareas del semáforo: {aggEx.InnerException?.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Esperando tareas: {ex.Message}");
                }

                Console.WriteLine("Volviendo al menú...\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR CRÍTICO] En RunTrafficLightAsync: {ex.Message}");
            }
        }

        // =========================================================
        //  UTIL
        // =========================================================
        static void LogDeadline(string task, TimeSpan elapsed, TimeSpan deadline, string msg)
        {
            try
            {
                if (string.IsNullOrEmpty(task))
                    task = "UNKNOWN";
                if (string.IsNullOrEmpty(msg))
                    msg = "Sin descripción";

                var ok = elapsed <= deadline;
                var status = ok ? "OK" : "MISS";
                Console.WriteLine($"{Stamp()} [{task}] [{status}] {elapsed.TotalMilliseconds:F1}ms (DL {deadline.TotalMilliseconds:F0}ms) -> {msg}");
            }
            catch (FormatException fEx)
            {
                Console.WriteLine($"[ERROR] Formato en LogDeadline: {fEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] En LogDeadline: {ex.Message}");
            }
        }

        static string Stamp() => $"[{DateTime.UtcNow:HH:mm:ss.fff}Z]";
    }
}
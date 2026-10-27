using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SimuladorBarberia
{
    public enum TipoServicio
    {
        CorteClasico = 30,
        CorteYBarba = 45,
        DegradadoFade = 40,
        Afeitado = 20,
        Tinte = 60,
        PerfiladoCejas = 15,
        TratamientoCapilar = 50
    }

    public class Cliente
    {
        public int Id { get; set; }
        public TipoServicio Servicio { get; set; }
        public int TiempoLlegada { get; set; }
        public int ToleranciaEspera { get; set; } // Minutos máximos antes de abandonar
        public int TiempoEsperado { get; set; } = 0;
    }

    public class Barbero
    {
        public int Id { get; set; }
        public Cliente ClienteActual { get; set; }
        public int MinutosOcupadoRestantes { get; set; } = 0;
        public int TotalMinutosTrabajados { get; set; } = 0;

        public bool EstaDisponible => MinutosOcupadoRestantes <= 0;
    }

    public class Simulador
    {
        private const int HORA_APERTURA = 8 * 60;  // 8:00 AM (480 min)
        private const int HORA_CIERRE = 18 * 60;   // 6:00 PM (1080 min)
        private const int UMBRAL_NUEVO_BARBERO = 3;
        private const int MAX_BARBEROS = 5;

        private List<Barbero> barberos;
        private Queue<Cliente> colaEspera;
        private Random random;

        private int clientesAtendidos = 0;
        private int clientesAbandonan = 0;
        private int clientesReprogramados = 0;
        private int totalClientesGenerados = 0;

        public Simulador()
        {
            barberos = new List<Barbero> { new Barbero { Id = 1 } }; 
            colaEspera = new Queue<Cliente>();
            random = new Random();
        }

        public void Iniciar()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("💈 INICIANDO SIMULACIÓN DE BARBERÍA (8:00 a 18:00) 💈");
            Console.WriteLine("==================================================\n");

            for (int tiempoActual = HORA_APERTURA; tiempoActual <= HORA_CIERRE; tiempoActual++)
            {
                GenerarLlegadaCliente(tiempoActual);
                ProcesarAbandonos(tiempoActual);
                EvaluarContratacionDinamica(tiempoActual);
                AsignarClientesABarberos(tiempoActual);
         
            }

            MostrarReporteFinal();
        }

        private void GenerarLlegadaCliente(int tiempoActual)
        {
            if (random.NextDouble() < 0.08)
            {
                totalClientesGenerados++;
                var servicios = (TipoServicio[])Enum.GetValues(typeof(TipoServicio));
                var nuevoCliente = new Cliente
                {
                    Id = totalClientesGenerados,
                    Servicio = servicios[random.Next(servicios.Length)],
                    TiempoLlegada = tiempoActual,
                    ToleranciaEspera = random.Next(15, 60) // Tolerancia aleatoria entre 15 y 60 min
                };

                colaEspera.Enqueue(nuevoCliente);
                ImprimirEvento(tiempoActual, $"Llega Cliente #{nuevoCliente.Id} solicitando {nuevoCliente.Servicio} (Tolerancia: {nuevoCliente.ToleranciaEspera}m). En cola: {colaEspera.Count}", ConsoleColor.Cyan);
            }
        }

        private void ProcesarAbandonos(int tiempoActual)
        {
            int cantidadInicial = colaEspera.Count;
            // Filtramos los clientes que superaron su nivel de tolerancia de estrés
            var clientesPacientes = new Queue<Cliente>();

            while (colaEspera.Count > 0)
            {
                var cliente = colaEspera.Dequeue();
                cliente.TiempoEsperado++;

                if (cliente.TiempoEsperado > cliente.ToleranciaEspera)
                {
                    clientesAbandonan++;
                    ImprimirEvento(tiempoActual, $"🔴 Cliente #{cliente.Id} abandonó el local por estrés (esperó más de {cliente.ToleranciaEspera} min).", ConsoleColor.Red);
                }
                else
                {
                    clientesPacientes.Enqueue(cliente);
                }
            }

            colaEspera = clientesPacientes;
        }

        private void EvaluarContratacionDinamica(int tiempoActual)
        {
            if (colaEspera.Count >= UMBRAL_NUEVO_BARBERO && barberos.Count < MAX_BARBEROS)
            {
                var nuevoBarbero = new Barbero { Id = barberos.Count + 1 };
                barberos.Add(nuevoBarbero);
                ImprimirEvento(tiempoActual, $"⚡ Alta demanda detectada. ¡Barbero #{nuevoBarbero.Id} se incorpora a la jornada!", ConsoleColor.Magenta);
            }
        }

        private void AsignarClientesABarberos(int tiempoActual)
        {
            foreach (var barbero in barberos.Where(b => b.EstaDisponible))
            {
                if (colaEspera.Count > 0)
                {
                    var cliente = colaEspera.Peek();
                    int duracion = (int)cliente.Servicio;

                    if (tiempoActual + duracion > HORA_CIERRE)
                    {
                        cliente = colaEspera.Dequeue();
                        clientesReprogramados++;
                        ImprimirEvento(tiempoActual, $"⚠️ Cliente #{cliente.Id} reprogramado para mañana a las 8:00 AM (El servicio {cliente.Servicio} supera las 6:00 PM).", ConsoleColor.DarkYellow);
                        continue;
                    }

                    cliente = colaEspera.Dequeue();
                    barbero.ClienteActual = cliente;
                    barbero.MinutosOcupadoRestantes = duracion;

                    ImprimirEvento(tiempoActual, $"✂️ Barbero #{barbero.Id} comienza {cliente.Servicio} al Cliente #{cliente.Id} (Duración: {duracion}m)", ConsoleColor.Green);
                }
            }
        }

        private void AvanzarTrabajoBarberos()
        {
            foreach (var barbero in barberos.Where(b => !b.EstaDisponible))
            {
                barbero.MinutosOcupadoRestantes--;
                barbero.TotalMinutosTrabajados++;

                if (barbero.EstaDisponible)
                {
                    clientesAtendidos++;
                }
            }
        }

        private void ImprimirEvento(int minutos, string mensaje, ConsoleColor color)
        {
            TimeSpan tiempo = TimeSpan.FromMinutes(minutos);
            string horaFormateada = tiempo.ToString(@"hh\:mm");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{horaFormateada}] ");
            Console.ForegroundColor = color;
            Console.WriteLine(mensaje);
            Console.ResetColor();
        }

        private void MostrarReporteFinal()
        {
            int duracionJornada = HORA_CIERRE - HORA_APERTURA;

            Console.WriteLine("\n==================================================");
            Console.WriteLine("📊 REPORTE FINAL DE LA JORNADA");
            Console.WriteLine("==================================================");
            Console.WriteLine($"- Total Clientes Recibidos:  {totalClientesGenerados}");
            Console.WriteLine($"- Clientes Atendidos:        {clientesAtendidos}");
            Console.WriteLine($"- Abandonos por estrés:      {clientesAbandonan}");
            Console.WriteLine($"- Reprogramados al día sig:  {clientesReprogramados}");
            Console.WriteLine($"- Barberos Activos al final: {barberos.Count}");
            Console.WriteLine("\n📈 Ocupación por Barbero:");

            foreach (var b in barberos)
            {
                double porcentaje = Math.Round((double)b.TotalMinutosTrabajados / duracionJornada * 100, 2);
                Console.WriteLine($"   Barbero #{b.Id}: Trabajó {b.TotalMinutosTrabajados} minutos ({porcentaje}% de la jornada)");
            }
            Console.WriteLine("==================================================\n");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var simulador = new Simulador();
            simulador.Iniciar();
        }
    }
}
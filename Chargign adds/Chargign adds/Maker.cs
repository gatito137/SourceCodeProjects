﻿using System.Data;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace Principal {
    internal class Maker {
        internal class Vars {
            public connection.Slack slack = new connection.Slack();
            public connection.SQL execute = new connection.SQL();
            public common.UseCommon use = new common.UseCommon();
        }
        public void MakeAll(ref Boolean isSimulation) {
            Vars js = new Vars();
            DateTime _now = DateTime.Now;
            int[] assessedCharges = { 0, 0, 0};
            
            //js.slack.data.Clear();
            //js.slack.data.alertTitle.Append("Alert");
            //js.slack.data.subject.Append("Procedure alert info: the process has begun...");
            //js.slack.data.message.Append("Start time: ").Append(_now);
            //js.slack.data.warningColour = js.slack.data.getColour.Green;
            //js.slack.send();

            try {
                TryMake(ref js, ref assessedCharges);
            } catch(Exception e) {
                js.slack.sendError(e.ToString());
            }

            js.slack.data.Clear();
            js.slack.data.alertTitle.Append("Alert");
            js.slack.data.subject.Append("Procedure alert info: the process has been completed!");
            js.slack.data.message.Append("Outstanding receivables: ").AppendLine(assessedCharges[0].ToString());
            js.slack.data.message.Append("Collections made: ").AppendLine(assessedCharges[1].ToString());
            js.slack.data.message.Append("Expired cards: ").AppendLine(assessedCharges[2].ToString());
            js.slack.data.message.Append("Total execution time: ").Append(DateTime.Now - _now);
            js.slack.data.warningColour = js.slack.data.getColour.Green;
            js.slack.send();
        }

        /// <summary>
        /// Main method of collection
        /// </summary>
        /// <param name="js"></param>
        /// <param name="assessedCharges"></param>
        private void TryMake(ref Vars js, ref int[] assessedCharges) {
            js.use.query.Clear();
            js.use.query.AppendLine("declare @Parameter smalldatetime = dateadd(");
            js.use.query.AppendLine("    day, (");
            js.use.query.AppendLine("        select");
            js.use.query.AppendLine("            convert(int, Valor)");
            js.use.query.AppendLine("        from");
            js.use.query.AppendLine("            Parametro");
            js.use.query.AppendLine("        where");
            js.use.query.AppendLine("            CodigoParametro = 17");
            js.use.query.AppendLine("    ), (");
            js.use.query.AppendLine("        select");
            js.use.query.AppendLine("            isnull(");
            js.use.query.AppendLine("                max(v.Fecha),");
            js.use.query.AppendLine("                dateadd(");
            js.use.query.AppendLine("                    day,");
            js.use.query.AppendLine("                    -(select convert(int, Valor) from Parametro where CodigoParametro = 17),");
            js.use.query.AppendLine("                    getdate()");
            js.use.query.AppendLine("                )");
            js.use.query.AppendLine("            )");
            js.use.query.AppendLine("        from");
            js.use.query.AppendLine("            Venta as v");
            js.use.query.AppendLine("            inner join DetalleVentaServicio as dvs on dvs.CodigoVenta = v.CodigoVenta");
            js.use.query.AppendLine("        where");
            js.use.query.AppendLine("            dvs.CodigoServicio = 5");
            js.use.query.AppendLine("            and v.CodigoProducto = 0");
            js.use.query.AppendLine("    )");
            js.use.query.AppendLine(")");

            js.use.query.AppendLine("select");
            js.use.query.AppendLine("    cli.CodigoCliente as CustomerCode,");
            js.use.query.AppendLine("    case");
            js.use.query.AppendLine("        when");
            js.use.query.AppendLine("            datediff(");
            js.use.query.AppendLine("                month,");
            js.use.query.AppendLine("                getdate(),");
            js.use.query.AppendLine("                concat(");
            js.use.query.AppendLine("                    cli.VencimientoTarjeta % 10000,");
            js.use.query.AppendLine("                    '-',");
            js.use.query.AppendLine("                    (cli.VencimientoTarjeta - cli.VencimientoTarjeta % 10000) / 10000,");
            js.use.query.AppendLine("                    '-01'");
            js.use.query.AppendLine("                )");
            js.use.query.AppendLine("            ) = 1 then 0");
            js.use.query.AppendLine("        when");
            js.use.query.AppendLine("            datediff(");
            js.use.query.AppendLine("                month,");
            js.use.query.AppendLine("                getdate(),");
            js.use.query.AppendLine("                concat(");
            js.use.query.AppendLine("                    cli.VencimientoTarjeta % 10000,");
            js.use.query.AppendLine("                    '-',");
            js.use.query.AppendLine("                    (cli.VencimientoTarjeta - cli.VencimientoTarjeta % 10000) / 10000,");
            js.use.query.AppendLine("                    '-01'");
            js.use.query.AppendLine("                )");
            js.use.query.AppendLine("            ) <= 0 then -1");
            js.use.query.AppendLine("        else");
            js.use.query.AppendLine("            iif(datediff(day, getdate(), @Parameter) < 1, 0, 1)");
            js.use.query.AppendLine("    end as Charging");
            js.use.query.AppendLine("from");
            js.use.query.AppendLine("    Click as c");
            js.use.query.AppendLine("    inner join Anuncio as a on a.CodigoAnuncio = c.CodigoAnuncio");
            js.use.query.AppendLine("    inner join Cliente as cli on cli.CodigoCliente = a.CodigoCliente");
            js.use.query.AppendLine("where");
            js.use.query.AppendLine("    c.CodigoVenta is null");
            js.use.query.AppendLine("    and isnull(cli.AnunciosSuspendidos, 0) = 0");
            js.use.query.AppendLine("    and isnull(a.Aprobado, 0) = 1");
            js.use.query.AppendLine("    and a.CodigoEstadoDeAnuncio = 1");
            js.use.query.AppendLine("group by");
            js.use.query.AppendLine("    cli.CodigoCliente,");
            js.use.query.AppendLine("    cli.VencimientoTarjeta");

            DataTable tabPrincipal = new DataTable();
            js.execute.fillTable(ref js.use.query, ref tabPrincipal);

            if (tabPrincipal.Rows.Count == 0) { return; }
            int Invoice = 0;
            String numerotarheta;
            String anio;
            String mes;

            foreach (DataRow row in tabPrincipal.Rows) {
                switch (int.Parse(row["Charging"].ToString())) {
                    case 1:
                        //Next turn
                        assessedCharges[0]++;
                        break;
                    case 0:
                        //Create invoice
                        //Invoice = getNewInvoice(row["CustomerCode"].ToString(), ref js);
                        Invoice = 590715;
                         js.use.query.Clear();
                        js.use.query.Append("Select CodigoVenta,NumeroTarjeta, CodigoSeguridad,VencimientoTarjeta,TotalVenta from Venta where CodigoFactura=").AppendLine(Invoice.ToString());
                        DataTable tableDatosT = new DataTable();
                        js.execute.fillTable(ref js.use.query, ref tableDatosT);
                        foreach (DataRow row1 in tableDatosT.Rows)
                        {
                            mes= row1["VencimientoTarjeta"].ToString().Substring(0, 2);
                            anio = row1["VencimientoTarjeta"].ToString().Substring(2, 4);
                            numerotarheta = getNumerotarjeta(row1["NumeroTarjeta"].ToString());
                            var datos = new EnvioData(
                                row1["TotalVenta"].ToString(),
                                numerotarheta,
                                mes, 
                                anio, 
                                row1["CodigoSeguridad"].ToString(),
                                Invoice.ToString()
                                );
                            var Rpeticio = new PagoTarjeta();
                            Rpeticio = CobroPeti.AutoCredomatic(datos);
                            if (Rpeticio.Code==100)
                            {
                                var service = new wsGD.ServiceSoapClient();
                                String vCae = "";
                                 vCae = service.fuFacturar(Invoice.ToString(),"", true);
                                if (vCae.Contains("Error"))
                                {
                                    assessedCharges[1]++;
                                }
                                else
                                {
                                    assessedCharges[0]++;
                                }
                            }
                            else
                            {
                                assessedCharges[1]++;
                            }
                        }
                        //Hacer cobro
                        //Facturar y enviar factura
                        break;
                    case -1:
                        assessedCharges[2]++;
                        //Tarjeta vencida
                        //Enviar correo y solicitar renovación de tarjeta (Ver gd-plus)
                        break;
                }
            }
        }

        /// <summary>
        /// Creates invoice by customer code
        /// creates sales by advertisement and creates a bill for the total of sales
        /// </summary>
        /// <param name="CustomerCode"/>
        /// <param name="js"/>
        /// <returns></returns>
        private int getNewInvoice(string CustomerCode, ref Vars js) {
            js.use.query.Clear();
            //Create variables
            js.use.query.Append("declare @CustomerCode int = ").AppendLine(CustomerCode);
            js.use.query.AppendLine("declare @InvoiceCode int = (select Max(CodigoFactura) + 1 from Factura)");
            js.use.query.AppendLine("declare @customer_data table(");
            js.use.query.AppendLine("    _code int,");
            js.use.query.AppendLine("    _name varchar(200),");
            js.use.query.AppendLine("    _mail varchar(255),");
            js.use.query.AppendLine("    _address varchar(500),");
            js.use.query.AppendLine("    _phone varchar(50),");
            js.use.query.AppendLine("    _nit int");
            js.use.query.AppendLine(")");
            js.use.query.AppendLine("declare @card_data table(");
            js.use.query.AppendLine("    _number varchar(256),");
            js.use.query.AppendLine("    _expiry int,");
            js.use.query.AppendLine("    _name varchar(200),");
            js.use.query.AppendLine("    _code varbinary(256),");
            js.use.query.AppendLine("    _type varchar(50)");
            js.use.query.AppendLine(")");
            js.use.query.AppendLine("declare @advert_data table(");
            js.use.query.AppendLine("    _name varchar(50),");
            js.use.query.AppendLine("    _clicks int,");
            js.use.query.AppendLine("    _total decimal(5, 2)");
            js.use.query.AppendLine(")");

            //Fill variables
            js.use.query.AppendLine("insert into @customer_data");
            js.use.query.AppendLine("    select");
            js.use.query.AppendLine("        CodigoCliente,");
            js.use.query.AppendLine("        NombreFactura,");
            js.use.query.AppendLine("        Correo,");
            js.use.query.AppendLine("        DireccionFactura,");
            js.use.query.AppendLine("        Telefono,");
            js.use.query.AppendLine("        Nit");
            js.use.query.AppendLine("    from");
            js.use.query.AppendLine("        Cliente");
            js.use.query.AppendLine("    where");
            js.use.query.AppendLine("        CodigoCliente = @CustomerCode");
            js.use.query.AppendLine("insert into @card_data");
            js.use.query.AppendLine("    select");
            js.use.query.AppendLine("        NumeroTarjeta,");
            js.use.query.AppendLine("        VencimientoTarjeta,");
            js.use.query.AppendLine("        NombreTarjeta,");
            js.use.query.AppendLine("        Clave,");
            js.use.query.AppendLine("        TipoTarjeta");
            js.use.query.AppendLine("    from");
            js.use.query.AppendLine("        Cliente");
            js.use.query.AppendLine("    where");
            js.use.query.AppendLine("        CodigoCliente = @CustomerCode");
            js.use.query.AppendLine("insert into @advert_data");
            js.use.query.AppendLine("    select");
            js.use.query.AppendLine("        a.Nombre,");
            js.use.query.AppendLine("        count(cl.CodigoAnuncio),");
            js.use.query.AppendLine("        convert(decimal(7, 2), sum(cl.CPC))");
            js.use.query.AppendLine("    from");
            js.use.query.AppendLine("        Click as cl");
            js.use.query.AppendLine("        inner join Anuncio as a on a.CodigoAnuncio = cl.CodigoAnuncio");
            js.use.query.AppendLine("        inner join Cliente as c on c.CodigoCliente = a.CodigoCliente");
            js.use.query.AppendLine("    where");
            js.use.query.AppendLine("        isnull(c.AnunciosSuspendidos, 0) = 0");
            js.use.query.AppendLine("        and cl.CodigoVenta is null");
            js.use.query.AppendLine("        and isnull(a.Aprobado, 0) = 1");
            js.use.query.AppendLine("        and a.CodigoEstadoDeAnuncio = 1");
            js.use.query.AppendLine("        and c.CodigoCliente = @CustomerCode");
            js.use.query.AppendLine("    group by");
            js.use.query.AppendLine("        a.Nombre");

            //Insert invoice
            js.use.query.AppendLine("insert into Factura(");
            js.use.query.AppendLine("    CodigoFactura,");
            js.use.query.AppendLine("    SerieDeFactura,");
            js.use.query.AppendLine("    NumeroDeFactura,");
            js.use.query.AppendLine("    Nit,");
            js.use.query.AppendLine("    Nombre,");
            js.use.query.AppendLine("    Direccion,");
            js.use.query.AppendLine("    CodigoEmpresa,");
            js.use.query.AppendLine("    CodigoEstadoFactura,");
            js.use.query.AppendLine("    Fecha,");
            js.use.query.AppendLine("    Total,");
            js.use.query.AppendLine("    TotalSinComisiones,");
            js.use.query.AppendLine("    Observaciones,");
            js.use.query.AppendLine("    CodigoCliente,");
            js.use.query.AppendLine("    TotalFactura,");
            js.use.query.AppendLine("    Telefonos,");
            js.use.query.AppendLine("    TotalPendiente,");
            js.use.query.AppendLine("    MontoServicioEnEfectivo,");
            js.use.query.AppendLine("    NombreRecibido,");
            js.use.query.AppendLine("    EsFacturaElectronica,");
            js.use.query.AppendLine("    DatosDeReembolsoPendientes,");
            js.use.query.AppendLine("    CodigoUsuarioDespacho,");
            js.use.query.AppendLine("    Envio,");
            js.use.query.AppendLine("    FechaPago");
            js.use.query.AppendLine(")values(");
            js.use.query.AppendLine("    @InvoiceCode,");
            js.use.query.AppendLine("    'Temp',");
            js.use.query.AppendLine("    1000000000,");
            js.use.query.AppendLine("    (select _nit from @customer_data),");
            js.use.query.AppendLine("    (select _name from @customer_data),");
            js.use.query.AppendLine("    (select _address from @customer_data),");
            js.use.query.AppendLine("    1,");
            js.use.query.AppendLine("    0,");
            js.use.query.AppendLine("    getdate(),");
            js.use.query.AppendLine("    (select sum(_total) from @advert_data),");
            js.use.query.AppendLine("    (select sum(_total) from @advert_data),");
            js.use.query.AppendLine("    '',");
            js.use.query.AppendLine("    (select _code from @customer_data),");
            js.use.query.AppendLine("    (select sum(_total) from @advert_data),");
            js.use.query.AppendLine("    (select _phone from @customer_data),");
            js.use.query.AppendLine("    0.00,");
            js.use.query.AppendLine("    0.00,");
            js.use.query.AppendLine("    'Auto',");
            js.use.query.AppendLine("    1,");
            js.use.query.AppendLine("    0,");
            js.use.query.AppendLine("    0,");
            js.use.query.AppendLine("    0.00,");
            js.use.query.AppendLine("    getdate()");
            js.use.query.AppendLine(")");

            //Insert sale
            js.use.query.AppendLine("insert into Venta(");
            js.use.query.AppendLine("    Fecha,");
            js.use.query.AppendLine("    NombreCliente,");
            js.use.query.AppendLine("    CorreoCliente,");
            js.use.query.AppendLine("    Cantidad,");
            js.use.query.AppendLine("    CodigoProducto,");
            js.use.query.AppendLine("    NombreProducto,");
            js.use.query.AppendLine("    Monto,");
            js.use.query.AppendLine("    CodigoFormaDePago,");
            js.use.query.AppendLine("    DireccionDeEntrega,");
            js.use.query.AppendLine("    Factura,");
            js.use.query.AppendLine("    Confirmada,");
            js.use.query.AppendLine("    Telefonos,");
            js.use.query.AppendLine("    NumeroTarjeta,");
            js.use.query.AppendLine("    VencimientoTarjeta,");
            js.use.query.AppendLine("    NombreTarjeta,");
            js.use.query.AppendLine("    Cuotas,");
            js.use.query.AppendLine("    MontoCuota,");
            js.use.query.AppendLine("    EnPreorden,");
            js.use.query.AppendLine("    -- CodigoSeguridad,");
            js.use.query.AppendLine("    CodigoRedCrediticia,");
            js.use.query.AppendLine("    CodigoTipoDeTarjeta,");
            js.use.query.AppendLine("    Pendiente,");
            js.use.query.AppendLine("    CodigoEstadoDeVenta,");
            js.use.query.AppendLine("    ProductoVerificado,");
            js.use.query.AppendLine("    CodigoFactura,");
            js.use.query.AppendLine("    MontoProveedor,");
            js.use.query.AppendLine("    CodigoCliente,");
            js.use.query.AppendLine("    Despachada,");
            js.use.query.AppendLine("    TotalVenta,");
            js.use.query.AppendLine("    FechaConfirmacion,");
            js.use.query.AppendLine("    Autenticada,");
            js.use.query.AppendLine("    UsuarioValidacion,");
            js.use.query.AppendLine("    FechaVerificacion,");
            js.use.query.AppendLine("    UsuarioVerificacion,");
            js.use.query.AppendLine("    EsDevolucion");
            js.use.query.AppendLine(")");
            js.use.query.AppendLine("    select");
            js.use.query.AppendLine("        getdate(),");
            js.use.query.AppendLine("        (select _name from @customer_data),");
            js.use.query.AppendLine("        (select _mail from @customer_data),");
            js.use.query.AppendLine("        _clicks,");
            js.use.query.AppendLine("        0,");
            js.use.query.AppendLine("        _name,");
            js.use.query.AppendLine("        _total,");
            js.use.query.AppendLine("        3,");
            js.use.query.AppendLine("        (select _address from @customer_data),");
            js.use.query.AppendLine("        '  NIT:   DIR: ',");
            js.use.query.AppendLine("        1,");
            js.use.query.AppendLine("        (select _phone from @customer_data),");
            js.use.query.AppendLine("        (select _number from @card_data),");
            js.use.query.AppendLine("        (select _expiry from @card_data),");
            js.use.query.AppendLine("        (select _name from @card_data),");
            js.use.query.AppendLine("        1,");
            js.use.query.AppendLine("        _total * (select ct.PorcentajeComision from CuotaTarjeta as ct where ct.CodigoRedCrediticia = 2 and ct.Cuotas = 1),");
            js.use.query.AppendLine("        0,");
            js.use.query.AppendLine("        -- (select _code from @card_data),");
            js.use.query.AppendLine("        1,");
            js.use.query.AppendLine("        (select _type from @card_data),");
            js.use.query.AppendLine("        0,");
            js.use.query.AppendLine("        1,");
            js.use.query.AppendLine("        1,");
            js.use.query.AppendLine("        @InvoiceCode,");
            js.use.query.AppendLine("        _total,");
            js.use.query.AppendLine("        (select _code from @customer_data),");
            js.use.query.AppendLine("        1,");
            js.use.query.AppendLine("        _total,");
            js.use.query.AppendLine("        getdate(),");
            js.use.query.AppendLine("        0,");
            js.use.query.AppendLine("        0,");
            js.use.query.AppendLine("        getdate(),");
            js.use.query.AppendLine("        0,");
            js.use.query.AppendLine("        0");
            js.use.query.AppendLine("    from");
            js.use.query.AppendLine("        @advert_data");
            js.use.query.AppendLine("insert into DetalleVentaServicio");
            js.use.query.AppendLine("    select");
            js.use.query.AppendLine("        scope_identity(),");
            js.use.query.AppendLine("        5,");
            js.use.query.AppendLine("        null");

            //Insert collection
            js.use.query.AppendLine("insert into Cobro(");
            js.use.query.AppendLine("    CodigoCobro,");
            js.use.query.AppendLine("    Fecha,");
            js.use.query.AppendLine("    UsuarioCobro,");
            js.use.query.AppendLine("    NumeroTarjeta,");
            js.use.query.AppendLine("    VencimientoTarjeta,");
            js.use.query.AppendLine("    NombreTarjeta,");
            js.use.query.AppendLine("    Cuotas,");
            js.use.query.AppendLine("    MontoCuota,");
            js.use.query.AppendLine("    CodigoRedCrediticia,");
            js.use.query.AppendLine("    CodigoTipoDeTarjeta,");
            js.use.query.AppendLine("    UsuarioTarjeta,");
            js.use.query.AppendLine("    FechaConfirmacion,");
            js.use.query.AppendLine("    CodigoFactura,");
            js.use.query.AppendLine("    CodigoFormaDePago,");
            js.use.query.AppendLine("    Estado,");
            js.use.query.AppendLine("    CodigoUsuarioCreacion");
            js.use.query.AppendLine(")values(");
            js.use.query.AppendLine("    (select max(CodigoCobro) + 1 from Cobro),");
            js.use.query.AppendLine("    getdate(),");
            js.use.query.AppendLine("    0,");
            js.use.query.AppendLine("    (select _number from @card_data),");
            js.use.query.AppendLine("    (select _expiry from @card_data),");
            js.use.query.AppendLine("    (select _name from @card_data),");
            js.use.query.AppendLine("    1,");
            js.use.query.AppendLine("    (select sum(MontoCuota) from Venta where CodigoFactura = @InvoiceCode),");
            js.use.query.AppendLine("    1,");
            js.use.query.AppendLine("    (select _type from @card_data),");
            js.use.query.AppendLine("    0,");
            js.use.query.AppendLine("    getdate(),");
            js.use.query.AppendLine("    @InvoiceCode,");
            js.use.query.AppendLine("    3,");
            js.use.query.AppendLine("    1,");
            js.use.query.AppendLine("    0");
            js.use.query.AppendLine(")");

            js.use.query.AppendLine("select @InvoiceCode as InvoiceCode");

            return js.execute.getNat(ref js.use.query, -1);
        }


        private string getNumerotarjeta(string tarjeta)
        {
            Byte[] IV = ASCIIEncoding.ASCII.GetBytes("8 @GD b!");
            Byte[] EncryptionKey = Convert.FromBase64String("MTIzNDU2Nzg5MDEyMzQ1000000000000");
            Byte[] buffer = Convert.FromBase64String(tarjeta);
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = EncryptionKey;
            tdes.IV = IV;
            return Encoding.UTF8.GetString(tdes.CreateDecryptor().TransformFinalBlock(buffer, 0, buffer.Length));
        }

    }
}
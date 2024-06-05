# Tecnologias utilizadas

# Relatório

## 1. Coleta de métricas

### 1.1. Criação da solução inicial

Inicialmente, foi criada uma solução Web Application utilizando o comando `dotnet new web -o WebMetric` na CLI do dotnet, que cria um projeto base vazio com a estrutura necessária para o desenvolvimento de qualquer tipo de aplicação web.

O resultado foi o seguinte:

<p align="center">
<img src="./assets/1.png">
</p>

### 1.2 Adição dos packages necessários

Em seguida, os packages `OpenTelemetry.Exporter.Prometheus.AspNetCore` e `OpenTelemetry.Extensions.Hosting` foram adicionados na solução criada por meio do comando `dotnet add package`, que alternativamente pode ser substituído pela adição do package pelo NuGet Package Manager de forma manual e na interface da IDE. Esses pacotes são os que permitem o uso das classes e métodos para enviar métricas para o Prometheus e utilizar o OpenTelemetry para a criação de métricas, respectivamente.

O resultado foi o seguinte:

<p align="center">
<img src="./assets/2.png">
</p>

<p align="center">
<img src="./assets/3.png">
</p>

### 1.3. Criação do Program.cs

Agora com o ambiente preparado, o seguinte código foi implementado:
```csharp
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();

        builder.AddMeter("Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel");
        builder.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
    });
var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.MapGet("/", () => "Hello OpenTelemetry! ticks:"
                      + DateTime.Now.Ticks.ToString()[^3..]);

app.Run();
```

Esse código é relativamente simples, inicialmente existe a configuração da aplicação para coletar e expor métricas usando OpenTelemetry, essas métricas são exportadas para o Prometheus através do exportador Prometheus. Além disso, é adicionado um medidor para as métricas relacionadas ao servidor Kestrel e ao host ASP.NET Core, e definição de uma visão personalizada para a duração das solicitações HTTP, especificando limites de intervalo. Por fim, é definido um endpoint para o Prometheus coletar as métricas e configura uma rota para retornar uma mensagem com um valor de "ticks" do tempo atual.  

O resultado foi o seguinte:

<p align="center">
<img src="./assets/4.png">
</p>

### 1.4. Exibindo as métricas com o dotnet-counters

Nesta etapa, já com o dotnet-counters instalado, bastou executar a aplicação por meio do comando `dotnet run` e em um terminal apartado executar o comando de monitoramento das métricas definidas do dotnet-counters, que foi o seguinte `dotnet-counters monitor -n WebMetric --counters Microsoft.AspNetCore.Hosting`.

Depois disso, o endpoint padrão definido `/` foi executado por meio do ThunderClient a coleta de métricas.

<p align="center">
<img src="./assets/5.png">
</p>

O resultado foi o seguinte:

<p align="center">
<img src="./assets/6.png">
</p>

## 2. Enriquecimento de métricas

### 2.1. Criação do Program.cs

Inicialmente, para enriquecer as métricas, o código anterior foi substituído pelo seguinte:
```csharp
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

app.Use(async (context, next) =>
{
    var tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
    if (tagsFeature != null)
    {
        var source = context.Request.Query["utm_medium"].ToString() switch
        {
            "" => "none",
            "social" => "social",
            "email" => "email",
            "organic" => "organic",
            _ => "other"
        };
        tagsFeature.Tags.Add(new KeyValuePair<string, object?>("mkt_medium", source));
    }

    await next.Invoke();
});

app.MapGet("/", () => "Hello World!");

app.Run();
```

O código configura um aplicativo ASP.NET Core que adiciona um middleware para enriquecer métricas com dados adicionais. Esse middleware verifica se a solicitação HTTP possui um recurso de tags de métricas (IHttpMetricsTagsFeature), se estiver presente, ele adiciona uma tag com a chave "mkt_medium" e um valor baseado no parâmetro da query string utm_medium. O valor é determinado pelo valor do parâmetro: "social", "email", "organic", ou um valor padrão "other".

Em relação ao código anterior, este enriquece as métricas adicionando informações contextuais de marketing às solicitações HTTP, o que permite diferenciar a origem das solicitações com base em campanhas de marketing específicas.

O resultado foi o seguinte:

<p align="center">
<img src="./assets/7.png">
</p>

### 2.2. Exibindo as métricas com o dotnet-counters

Em seguida, similar a exibição de métricas anterior, bastou executar a aplicação por meio do comando `dotnet run` e em um terminal diferente executar o comando de monitoramento das métricas definidas do dotnet-counters, que foi o seguinte `dotnet-counters monitor -n WebMetric --counters Microsoft.AspNetCore.Hosting`.

Depois disso, o endpoint padrão definido `/` foi executado por meio do ThunderClient para a coleta de métricas.

<p align="center">
<img src="./assets/8.png">
</p>

<p align="center">
<img src="./assets/9.png">
</p>

O resultado foi o seguinte:

<p align="center">
<img src="./assets/10.png">
</p>

## 3. Criação de métricas personalizadas

### 3.1. Criação da classe ContosoMetrics

Aqui foi criada uma nova classe na solução, chamada de ContosoMetrics, que possui o método `ProductSold`, que adiciona o produto vendido ao counter criado anteriormente no construtor, que representa a métrica.

O resultado foi o seguinte:

<p align="center">
<img src="./assets/11.png">
</p>

### 3.2. Criação do Program.cs

Em seguida, o código do Program foi substituído pelo seguinte:
```csharp
using WebMetric;
using WebMetric.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ContosoMetrics>();

var app = builder.Build();

app.MapPost("/complete-sale", (SaleModel model, ContosoMetrics metrics) =>
{
    metrics.ProductSold(model.ProductName, model.QuantitySold);
});

app.Run();
```

Esse código registra a classe criada anteriormente com Dependency Injection(DI) e utiliza de seu método para registrar as métricas ao executar o endpoint tipo post `/complete-sale`.

O resultado foi o seguinte:

<p align="center">
<img src="./assets/12.png">
</p>

### 3.2. Criação do SaleModel

Com a finalidade de modularizar o tipo dos parâmetros que são passados para o método `ProductSold` dentro de Program.cs, foi criado um SaleModel com as propriedades `ProductName` e `QuantitySold` em uma pasta nova Models.

<p align="center">
<img src="./assets/13.png">
</p>

### 3.3. Exibindo as métricas com o dotnet-counters

Por fim, similar a exibição de métricas já mostrada, bastou executar a aplicação por meio do comando `dotnet run` e em um terminal diferente executar o comando de monitoramento das métricas definidas do dotnet-counters, que foi o seguinte `dotnet-counters monitor -n WebMetric --counters Contoso.Web`.

<p align="center">
<img src="./assets/14.png">
</p>

Depois disso, o endpoint padrão definido `/complete-sale` foi executado duas vezes por meio do ThunderClient para a coleta de métricas. Na primeira vez registrou a venda de 3 camisetas e na segunda, de 5 calças.

O resultado foi o seguinte:

<p align="center">
<img src="./assets/15.png">
</p>

<p align="center">
<img src="./assets/16.png">
</p>

<p align="center">
<img src="./assets/17.png">
</p>

<p align="center">
<img src="./assets/18.png">
</p>

## 4. Envio das métricas para o Grafana e Prometheus

### 4.1. Substituição para o código da solução inicial e execução

Nessa etapa, o código da etapa 1.3 foi reutilizado, já que inclui a parte do envio de métricas para o Prometheus.

Esse foi o resultado da aplicação rodando:
<p align="center">
<img src="./assets/19.png">
</p>

### 4.2. Observação das métricas no endpoint

Em seguida, por padrão, o endpoint `/metrics` é disponibilizado para a exibição de métricas, as mesmas que serão enviadas ao Prometheus.

<p align="center">
<img src="./assets/20.png">
</p>

### 4.3. Criação e execução do Docker Compose

Como alternativa a instalação local do Prometheus e Grafana, foi criado um `docker-compose.yml` que executa os dois serviços, seu conteúdo é o seguinte:
```yml
services:
  prometheus:
    image: prom/prometheus
    network_mode: "host"
    container_name: prometheus
    volumes:
      - ./prometheus:/etc/prometheus
  
  grafana:
    image: grafana/grafana
    network_mode: "host"
    container_name: grafana
```

Além disso, foi criada uma pasta prometheus com o arquivo `prometheus.yml`, que configura o Prometheus de acordo com o indicado nas instruções. No arquivo `docker-compose.yml`, é mapeado um diretório local (./prometheus) para o diretório /etc/prometheus dentro do container Prometheus, que garante que as configurações e dados do Prometheus criadas sejam preservados no sistema host, mesmo se o container for reiniciado ou removido.

<p align="center">
<img src="./assets/22.png">
</p>

### 4.4. Coleta das métricas no Prometheus

<p align="center">
<img src="./assets/23.png">
</p>

<p align="center">
<img src="./assets/24.png">
</p>

<p align="center">
<img src="./assets/25.png">
</p>

<p align="center">
<img src="./assets/26.png">
</p>

<p align="center">
<img src="./assets/27.png">
</p>

### 4.5. Dashboard de visualização das métricas no Grafana

<p align="center">
<img src="./assets/28.png">
</p>

<p align="center">
<img src="./assets/29.png">
</p>

<p align="center">
<img src="./assets/30.png">
</p>

<p align="center">
<img src="./assets/31.png">
</p>

<p align="center">
<img src="./assets/32.png">
</p>

<p align="center">
<img src="./assets/33.png">
</p>

<p align="center">
<img src="./assets/34.png">
</p>

<p align="center">
<img src="./assets/35.png">
</p>

<p align="center">
<img src="./assets/36.png">
</p>

<p align="center">
<img src="./assets/37.png">
</p>

# Conceitos aprendidos
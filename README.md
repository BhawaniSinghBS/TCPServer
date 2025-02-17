  builder.Services.AddSingleton<TcpServerService>();
  builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TcpServerService>());
  builder.Services.AddSingleton<IHostedService>(sp => new TCPServerTimer(sp.GetRequiredService<TcpServerService>(), configuration));

// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Open.IdentityServer.DynamicClientManagement.EntityFramework.Stores;
using Open.IdentityServer.EntityFramework.Interfaces;
using ClientEntity = Open.IdentityServer.EntityFramework.Entities.Client;
using ModelClient = Open.IdentityServer.Models.Client;

namespace Open.IdentityServer.DynamicClientManagement.EntityFramework.UnitTests.Stores;

public class DynamicClientManagementStoreTests
{
    [Fact]
    public async Task register_client_when_cancellation_requested_should_throw()
    {
        var clients = new List<ClientEntity>();
        var clientSet = CreateClientSet(clients);
        var context = CreateContext(clientSet.Object);
        var subject = CreateSubject(context.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => subject.RegisterClientAsync(
            new ModelClient { ClientId = "cancelled-client" },
            cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        context.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task register_client_when_client_is_null_should_throw()
    {
        var clients = new List<ClientEntity>();
        var clientSet = CreateClientSet(clients);
        var context = CreateContext(clientSet.Object);
        var subject = CreateSubject(context.Object);

        var act = () => subject.RegisterClientAsync(null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();

        context.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task register_client_when_client_already_exists_should_throw()
    {
        var clients = new List<ClientEntity>
        {
            new() { ClientId = "duplicate-client" }
        };
        var clientSet = CreateClientSet(clients);
        var context = CreateContext(clientSet.Object);
        var subject = CreateSubject(context.Object);

        var act = () => subject.RegisterClientAsync(
            new ModelClient { ClientId = "duplicate-client" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();

        clientSet.Verify(x => x.Add(It.IsAny<ClientEntity>()), Times.Never);
        context.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task register_client_when_client_does_not_exist_should_add_and_save()
    {
        var clients = new List<ClientEntity>();
        var clientSet = CreateClientSet(clients);
        var context = CreateContext(clientSet.Object);
        var subject = CreateSubject(context.Object);
        var client = new ModelClient
        {
            ClientId = "new-client",
            ClientName = "New Client"
        };

        await subject.RegisterClientAsync(client, TestContext.Current.CancellationToken);

        clients.Should().ContainSingle();
        clients[0].ClientId.Should().Be(client.ClientId);
        context.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private static DynamicClientManagementStore CreateSubject(IConfigurationDbContext context)
    {
        return new DynamicClientManagementStore(context, NullLogger<DynamicClientManagementStore>.Instance);
    }

    private static Mock<IConfigurationDbContext> CreateContext(DbSet<ClientEntity> clients)
    {
        var context = new Mock<IConfigurationDbContext>();
        context.SetupGet(x => x.Clients).Returns(clients);
        context.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        return context;
    }

    private static Mock<DbSet<ClientEntity>> CreateClientSet(IList<ClientEntity> clients)
    {
        var queryable = clients.AsQueryable();
        var clientSet = new Mock<DbSet<ClientEntity>>();

        clientSet.As<IQueryable<ClientEntity>>().Setup(x => x.Provider).Returns(queryable.Provider);
        clientSet.As<IQueryable<ClientEntity>>().Setup(x => x.Expression).Returns(queryable.Expression);
        clientSet.As<IQueryable<ClientEntity>>().Setup(x => x.ElementType).Returns(queryable.ElementType);
        clientSet.As<IQueryable<ClientEntity>>().Setup(x => x.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        clientSet.Setup(x => x.Add(It.IsAny<ClientEntity>()))
            .Callback<ClientEntity>(entity => clients.Add(entity));

        return clientSet;
    }
}
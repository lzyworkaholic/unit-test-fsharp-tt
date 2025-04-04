module Tests

open Xunit
open FsUnit.Xunit
open Moq
open MainProject
open System
open System.IO

module OrderProcessingServiceTests =
    let createOrder id orderType amount flag status priority = 
        { Id = id; Type = orderType; Amount = amount; Flag = flag; Status = status; Priority = priority }

    let setupStreamWriterMock () =
        let streamWriterMock = Mock<IStreamWriter>()
        streamWriterMock.Setup(fun x -> x.WriteLine(It.IsAny<string>())).Callback(fun (_: string) -> ()) |> ignore
        streamWriterMock.Setup(fun x -> x.Dispose()).Callback(fun () -> ()) |> ignore
        streamWriterMock

    let setupFactoryMock (streamWriterMock: Mock<IStreamWriter>) =
        let factoryMock = Mock<IStreamWriterFactory>()
        factoryMock.Setup(fun x -> x.Create(It.IsAny<string>())).Returns(streamWriterMock.Object) |> ignore
        factoryMock

    let setupApiMock (status: string, data: obj) =
        let apiMock = Mock<IAPIClient>()
        apiMock.Setup(fun x -> x.CallAPI(It.IsAny<int>())).Returns({ Status = status; Data = data }) |> ignore
        apiMock

    [<Fact>]
    let ``ProcessOrders returns false when no orders found`` () =
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([]) |> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.False(result)

    [<Fact>]
    let ``ProcessOrders handles Type A order with successful export`` () =
        let order = createOrder 1 "A" 100.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order]) |> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "exported", "low")).Returns(true) |> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type A order with high value export`` () =
        let order = createOrder 1 "A" 200.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order]) |> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "exported", "high")).Returns(true) |> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type A order with amount greater than 150`` () =
        let order = createOrder 1 "A" 151.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "exported", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type B order with successful API call and processed status`` () =
        let order = createOrder 1 "B" 50.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "processed", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 50)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type B order with data less than 50`` () =
        let order = createOrder 1 "B" 50.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "pending", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 40)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type B order with flag true`` () =
        let order = createOrder 1 "B" 150.0 true "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "pending", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 60)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type B order with error status`` () =
        let order = createOrder 1 "B" 150.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "error", "low")).Returns(true)|> ignore
        
        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 60)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type B order with API failure`` () =
        let order = createOrder 1 "B" 50.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "api_failure", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)

        let apiMock = Mock<IAPIClient>()
        apiMock.Setup(fun x -> x.CallAPI(1)).Throws(APIException "API failed")|> ignore

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type B order with API error status`` () =
        let order = createOrder 1 "B" 50.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "api_error", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("failed", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type C order with flag true`` () =
        let order = createOrder 1 "C" 50.0 true "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "completed", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type C order with flag false`` () =
        let order = createOrder 1 "C" 50.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "in_progress", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles unknown order type`` () =
        let order = createOrder 1 "D" 50.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "unknown_type", "low")).Returns(true)|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles database exception`` () =
        let order = createOrder 1 "C" 50.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order])|> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "in_progress", "low")).Throws(DatabaseException "DB failed")|> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object 1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles Type A order with export failure due to IOException`` () =
        let order = createOrder 1 "A" 100.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order]) |> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "export_failed", "low")).Returns(true) |> ignore

        let streamWriterMock = Mock<IStreamWriter>()
        streamWriterMock.Setup(fun x -> x.WriteLine(It.IsAny<string>())).Callback(fun (s: string) -> raise (IOException "File write failed")) |> ignore
        streamWriterMock.Setup(fun x -> x.Dispose()).Callback(fun () -> ()) |> ignore
        
        let factoryMock = Mock<IStreamWriterFactory>()
        factoryMock.Setup(fun x -> x.Create(It.IsAny<string>())).Returns(streamWriterMock.Object) |> ignore

        let apiMock = setupApiMock("success", box 0)

        let result = OrderProcessingService.processOrders dbMock.Object apiMock.Object  1 factoryMock.Object
        Assert.True(result)

    [<Fact>]
    let ``ProcessOrders handles outer exception case`` () =
        // Arrange
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1))
            .Throws(Exception("Unexpected error")) |> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        // Act
        let result = OrderProcessingService.processOrders 
                        dbMock.Object apiMock.Object 1 factoryMock.Object

        // Assert
        Assert.False(result)

    [<Fact>]
    let ``ProcessOrders handles Type A order with successful export and high amount`` () =
        // Arrange
        let order = createOrder 1 "A" 201.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order]) |> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(1, "exported", "high")).Returns(true) |> ignore

        let streamWriterMock = Mock<IStreamWriter>()
        let writtenLines = ResizeArray<string>()
        streamWriterMock.Setup(fun x -> x.WriteLine(It.IsAny<string>()))
                        .Callback(fun (s: string) -> writtenLines.Add(s)) |> ignore
        streamWriterMock.Setup(fun x -> x.Dispose()).Callback(fun () -> ()) |> ignore
        
        let factoryMock = Mock<IStreamWriterFactory>()
        factoryMock.Setup(fun x -> x.Create(It.IsAny<string>()))
                .Returns(streamWriterMock.Object) |> ignore

        let apiMock = setupApiMock("success", box 0)

        // Act
        let result = OrderProcessingService.processOrders 
                        dbMock.Object apiMock.Object 1 factoryMock.Object

        // Assert
        Assert.True(result)
        Assert.Equal(3, writtenLines.Count) // Header + Data + High value note
        Assert.Contains("Note,High value order", writtenLines.[2])

    [<Fact>]
    let ``ProcessOrders handles multiple database exceptions`` () =
        // Arrange
        let order = createOrder 1 "C" 50.0 false "pending" "low"
        let dbMock = Mock<IDatabaseService>()
        dbMock.Setup(fun x -> x.GetOrdersByUser(1)).Returns([order]) |> ignore
        dbMock.Setup(fun x -> x.UpdateOrderStatus(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(DatabaseException "Persistent DB Error") |> ignore

        let streamWriterMock = setupStreamWriterMock()
        let factoryMock = setupFactoryMock(streamWriterMock)
        let apiMock = setupApiMock("success", box 0)

        // Act & Assert
        let result = OrderProcessingService.processOrders 
                        dbMock.Object apiMock.Object 1 factoryMock.Object
        
        Assert.True(result)
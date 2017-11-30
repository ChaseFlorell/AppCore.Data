﻿using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using FutureState.AppCore.Data.Tests.Helpers.Fixtures;
using FutureState.AppCore.Data.Tests.Helpers.Models;
using NUnit.Framework;

namespace FutureState.AppCore.Data.Tests.Integration
{
    public class SimpleCrudTransactionTests : IntegrationTestBase
    {
        [Test, TestCaseSource(nameof(DbProviders))]
        public void Should_Do_CUD_In_Transactions(IDbProvider db)
        {
            Trace.WriteLine(TraceObjectGraphInfo(db));

            // create
            var motorcycle = AutomobileFixture.GetMotorcycle();
            var car = AutomobileFixture.GetCar();
            db.RunInTransaction(transaction =>
            {
                transaction.Create(motorcycle); // create only
                transaction.CreateOrUpdate(car); // create or update
            });

            // assert create
            var actualMotorcycle = db.Query<AutomobileModel>().Where(a => a.Vin == motorcycle.Vin).SingleOrDefault();
            var actualCar = db.Query<AutomobileModel>().Where(a => a.Vin == car.Vin).SingleOrDefault();
            actualMotorcycle.Should().NotBeNull();
            actualCar.Should().NotBeNull();

            // update
            motorcycle.VehicleType = "scooter";
            car.VehicleType = "truck";
            db.RunInTransaction(transaction =>
            {
                transaction.CreateOrUpdate(motorcycle); // create or update
                transaction.Update(car); // update only
            });

            // assert update
            actualMotorcycle = db.Query<AutomobileModel>().Where(a => a.Vin == motorcycle.Vin).SingleOrDefault();
            actualCar = db.Query<AutomobileModel>().Where(a => a.Vin == car.Vin).SingleOrDefault();
            actualMotorcycle.Should().NotBeNull();
            actualCar.Should().NotBeNull();
            actualMotorcycle.VehicleType.Should().Be(motorcycle.VehicleType);
            actualCar.VehicleType.Should().Be(car.VehicleType);

            // delete
            db.RunInTransaction(transaction =>
            {
                transaction.Delete<AutomobileModel>(a => a.Vin == motorcycle.Vin);
                transaction.Delete<AutomobileModel>(a => a.Vin == car.Vin);
            });

            // assert delete
            actualMotorcycle = db.Query<AutomobileModel>().Where(a => a.Vin == motorcycle.Vin).SingleOrDefault();
            actualCar = db.Query<AutomobileModel>().Where(a => a.Vin == car.Vin).SingleOrDefault();
            actualMotorcycle.Should().BeNull();
            actualCar.Should().BeNull();
        }

        [Test, TestCaseSource(nameof(DbProviders))]
        public void Should_Perform_Faster_When_Run_In_Transaction(IDbProvider db)
        {
            Trace.WriteLine(TraceObjectGraphInfo(db));

            // setup
            var carWatch = new Stopwatch();
            var bikeWatch = new Stopwatch();

            // transaction test
            var car = AutomobileFixture.GetCar();
            carWatch.Start();
            db.RunInTransaction(trans =>
            {
                for (var i = 10; i < 1000; i++) // 990 records
                {
                    car.Vin = i.ToString();
                    trans.CreateOrUpdate(car);
                }
            });
            carWatch.Stop();

            // non transaction test
            var motorcycle = AutomobileFixture.GetMotorcycle();
            bikeWatch.Start();
            for (var i = 1010; i < 2000; i++) // 990 records
            {
                motorcycle.Vin = i.ToString();
                db.CreateOrUpdate(motorcycle);
            }
            bikeWatch.Stop();
            carWatch.ElapsedTicks.Should().BeLessThan(bikeWatch.ElapsedTicks);

            // assert record count
            var vehicleCount = db.Query<AutomobileModel>().ToList().Count;
            vehicleCount.Should().Be(1980);

            Trace.WriteLine($"Non Transaction: {bikeWatch.Elapsed.ToString(@"hh\:mm\:ss")} \t(Ticks {bikeWatch.ElapsedTicks})");
            Trace.WriteLine($"Transaction: {carWatch.Elapsed.ToString(@"hh\:mm\:ss")} \t\t(Ticks {carWatch.ElapsedTicks})");
        }

        [Test, TestCaseSource(nameof(DbProviders))]
        public void Should_Create_Records_With_AutoIncrement(IDbProvider db)
        {
            Trace.WriteLine(TraceObjectGraphInfo(db));

            // setup
            var foos = new List<FooModel>();
            for (var i = 1; i < 21; i++)
            {
                foos.Add(new FooModel { Name = $"Name-{i}" });
            }

            // execute
            foreach (var foo in foos)
            {
                db.Create(foo);
            }
            var actualFoos = db.Query<FooModel>().ToList();
            actualFoos.Count.Should().Be(20);
        }
    }
}
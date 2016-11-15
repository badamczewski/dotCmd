using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using dotCmd.DataStructures;
using System.Linq;

namespace dotCmd.Tests
{
    [TestClass]
    public class DotConsoleTests
    {
        [TestMethod]
        public void DotConsole_Should_Write_Text_To_The_OutputBuffer()
        {
            //Arrange
            var actual = "Hello World";
            DotConsole console = new DotConsole();

            //Act
            var @ref = console.Write(actual);

            //Assert
            //Use the renderer and load the line.
            var exp = console.Renderer.ReadOutput(new Region() { Left = 0, Top = 0, Height = 1, Width = actual.Length });
            CellBuffer.CellBufferDebugView view = new CellBuffer.CellBufferDebugView(exp);

            Assert.AreEqual(view.lines[0], actual);
        }

        [TestMethod]
        public void DotConsole_Should_WriteLine_Of_Text_To_The_OutputBuffer()
        {
            //Arrange
            var actual = "Hello World";
            DotConsole console = new DotConsole();

            //Act
            var @ref = console.WriteLine(actual);
            @ref = console.Write(actual);

            //Assert
            //Use the renderer and load the line.
            var exp = console.Renderer.ReadOutput(new Region() { Left = 0, Top = 0, Height = @ref.RelativeRowIndex + 1, Width = actual.Length });
            CellBuffer.CellBufferDebugView view = new CellBuffer.CellBufferDebugView(exp);

            view.lines.ToList()
                .ForEach(line => Assert.AreEqual(line, actual));
        }

        [TestMethod]
        public void DotConsole_Should_AlterLine_Of_Text_In_The_OutputBuffer()
        {
            //Arrange
            var line = "Hello";
            var actual = "World";
            DotConsole console = new DotConsole();

            //Act
            var @ref = console.WriteLine(line);

            console.AlterLine(actual, @ref.RelativeRowIndex);

            //Assert
            //Use the renderer and load the line.
            var exp = console.Renderer.ReadOutput(new Region() { Left = 0, Top = 0, Height = @ref.RelativeRowIndex + 1, Width = line.Length });
            CellBuffer.CellBufferDebugView view = new CellBuffer.CellBufferDebugView(exp);

            Assert.AreEqual(view.lines[0], actual);
        }

        [TestMethod]
        public void DotConsole_Should_AlterText_In_The_OutputBuffer()
        {
            //Arrange
            var line = "Hello";
            var actual = "World";
            DotConsole console = new DotConsole();

            //Act
            var @ref = console.Write(line);

            console.AlterLine(actual, @ref.RelativeColIndex);

            //Assert
            //Use the renderer and load the line.
            var exp = console.Renderer.ReadOutput(new Region() { Left = 0, Top = @ref.RelativeColIndex, Height = @ref.RelativeRowIndex + 1, Width = line.Length });
            CellBuffer.CellBufferDebugView view = new CellBuffer.CellBufferDebugView(exp);

            Assert.AreEqual(view.lines[0], actual);
        }

        [TestMethod]
        public void DotConsole_Should_WriteLine_Of_Text_With_Colors_To_The_OutputBuffer()
        {
            //Arrange
            var text = "Hello World";
            DotConsole console = new DotConsole();

            //Setup colors
            var actualFirst = new Color(50, 50, 50);
            var actualSecond = new Color(100,100,100);

            //Act
            var @ref = console.WriteLine(text, actualFirst, actualSecond, false);

            //Assert
            //Use the renderer and load the line.
            var exp = console.Renderer.ReadOutput(new Region() { Left = 0, Top = 0, Height = @ref.RelativeRowIndex + 1, Width = text.Length });
            CellBuffer.CellBufferDebugView view = new CellBuffer.CellBufferDebugView(exp);

            //The colors should be in the renderer map and correspond to ConsoleColor keys.
            ConsoleColor expectedFirst = ConsoleColor.Black;
            ConsoleColor expectedSecond = ConsoleColor.Black;
            console.Renderer.ColorMap.TryGetMappedColor(actualFirst, out expectedFirst);
            console.Renderer.ColorMap.TryGetMappedColor(actualSecond, out expectedSecond);

            //We know the color keys since they are int and increment by one each time.
            Assert.AreEqual(expectedFirst, ConsoleColor.DarkGreen);
            Assert.AreEqual(expectedSecond, ConsoleColor.DarkBlue);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Entities.Tests;
using Unity.Collections;
using Unity.Entities;
using DotsUI.Controls;
using DotsUI.Input;
using DotsUI.Core;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsUI.Controls.Tests
{
    [TestFixture]
    public class InputFieldTests : ECSTestsFixture
    {
        private Entity InstantiateInputFieldWithFocus(string textBuffer = "")
        {
            var entity = m_Manager.CreateEntity(typeof(InputField), typeof(TextData), typeof(InputFieldCaretState));
            m_Manager.SetComponentData(entity, new InputField()
            {
                Placeholder = default,
                Target = entity
            });
            if (textBuffer.Length > 0)
            {
                var buff = m_Manager.GetBuffer<TextData>(entity);
                buff.ResizeUninitialized(textBuffer.Length);
                unsafe
                {
                    fixed (char* textPtr = textBuffer)
                        UnsafeUtility.MemCpy(buff.GetUnsafePtr(), textPtr, textBuffer.Length * sizeof(char));
                }
            }

            return entity;
        }

        public KeyboardInputBuffer KeyboardInput(char c)
        {
            return new KeyboardInputBuffer() { Character = c, EventType = KeyboardEventType.Character };
        }
        public KeyboardInputBuffer KeyboardInput(KeyCode c)
        {
            return new KeyboardInputBuffer() { Character = '\0', EventType = KeyboardEventType.Key, KeyCode = (ushort)c };
        }

        public NativeArray<KeyboardInputBuffer> KeyboardInput(string str)
        {
            NativeArray<KeyboardInputBuffer> ret = new NativeArray<KeyboardInputBuffer>(str.Length, Allocator.Temp);
            for (int i = 0; i < str.Length; i++)
                ret[i] = KeyboardInput(str[i]);
            return ret;
        }

        public string GetStringFormEntityBuffer(Entity entity)
        {
            unsafe
            {
                var buffer = m_Manager.GetBuffer<TextData>(entity);
                return System.Text.Encoding.Unicode.GetString((byte*)buffer.GetUnsafePtr(), buffer.Length * 2);
            }
        }


        private Entity SpawnKeyboardEvent(Entity target)
        {
            var ret = m_Manager.CreateEntity(typeof(KeyboardEvent), typeof(KeyboardInputBuffer));
            m_Manager.SetComponentData(ret, new KeyboardEvent
            {
                Target = target
            });
            return ret;
        }

        [Test]
        public void KeyboardParseBackspace()
        {
            var emptyInputField = InstantiateInputFieldWithFocus();
            Entity eventEntity = SpawnKeyboardEvent(emptyInputField);
            var keyboardBuffer = m_Manager.GetBuffer<KeyboardInputBuffer>(eventEntity);
            keyboardBuffer.AddRange(KeyboardInput("Test String"));
            keyboardBuffer.Add(KeyboardInput(KeyCode.Backspace));
            keyboardBuffer.Add(KeyboardInput(KeyCode.Backspace));
            keyboardBuffer.Add(KeyboardInput(KeyCode.Backspace));
            keyboardBuffer.Add(KeyboardInput(KeyCode.Backspace));
            var inputFieldSystem = World.GetOrCreateSystem<InputFieldSystem>();
            inputFieldSystem.Update();
            m_Manager.CompleteAllJobs();

            Assert.AreEqual("Test St", GetStringFormEntityBuffer(emptyInputField));
        }

        [Test]
        public void KeyboardParseLRArrows()
        {
            var emptyInputField = InstantiateInputFieldWithFocus();
            Entity eventEntity = SpawnKeyboardEvent(emptyInputField);
            var keyboardBuffer = m_Manager.GetBuffer<KeyboardInputBuffer>(eventEntity);
            keyboardBuffer.AddRange(KeyboardInput("Test String"));
            keyboardBuffer.Add(KeyboardInput(KeyCode.LeftArrow));
            keyboardBuffer.Add(KeyboardInput(KeyCode.LeftArrow));
            keyboardBuffer.AddRange(KeyboardInput("!!!!"));
            keyboardBuffer.Add(KeyboardInput(KeyCode.RightArrow));
            keyboardBuffer.Add(KeyboardInput(KeyCode.RightArrow));
            keyboardBuffer.Add(KeyboardInput(KeyCode.RightArrow));
            keyboardBuffer.Add(KeyboardInput(KeyCode.RightArrow));
            keyboardBuffer.AddRange(KeyboardInput("???"));
            var inputFieldSystem = World.GetOrCreateSystem<InputFieldSystem>();
            inputFieldSystem.Update();
            m_Manager.CompleteAllJobs();

            Assert.AreEqual("Test Stri!!!!ng???", GetStringFormEntityBuffer(emptyInputField));
        }

        [Test]
        public void KeyboardParseHomeEnd()
        {
            var emptyInputField = InstantiateInputFieldWithFocus();
            Entity eventEntity = SpawnKeyboardEvent(emptyInputField);
            var keyboardBuffer = m_Manager.GetBuffer<KeyboardInputBuffer>(eventEntity);
            keyboardBuffer.AddRange(KeyboardInput("Test String"));
            keyboardBuffer.Add(KeyboardInput(KeyCode.Home));
            keyboardBuffer.AddRange(KeyboardInput("!!!!"));
            keyboardBuffer.Add(KeyboardInput(KeyCode.End));
            keyboardBuffer.AddRange(KeyboardInput("???"));
            var inputFieldSystem = World.GetOrCreateSystem<InputFieldSystem>();
            inputFieldSystem.Update();
            m_Manager.CompleteAllJobs();

            Assert.AreEqual("!!!!Test String???", GetStringFormEntityBuffer(emptyInputField));
        }
    }
}

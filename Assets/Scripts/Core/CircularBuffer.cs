using System;
using System.Text;
using System.Collections.Generic;

public class CircularBuffer<T> where T : IEquatable<T>
{
    private const int BUFFER_EMPTY_WRITE_IDX = -1;
    public T DefaultValue { get; private set; }
    private CircularBuffer()
    {
        throw new InvalidOperationException();
    }
    public CircularBuffer(int capacity)
    {
        if (capacity == 0)
        {
            throw new IndexOutOfRangeException("Unable to create a 0 length circular buffer.");
        }
        StartIndex = 0;
        WriteIndex = BUFFER_EMPTY_WRITE_IDX;

        values = new T[capacity];
    }
    public CircularBuffer(int capacity, T defaultValue)
    {
        if (capacity == 0)
        {
            throw new IndexOutOfRangeException("Unable to create a 0 length circular buffer.");
        }
        StartIndex = 0;
        WriteIndex = BUFFER_EMPTY_WRITE_IDX;

        values = new T[capacity];

        for (int i = 0; i < capacity; ++i)
        {
            values[i] = defaultValue;
        }
        DefaultValue = defaultValue;
    }

    private T[] values;
    public int Count
    {
        get
        {
            if (IsEmpty())
            {
                return 0;
            }
            else if (WriteIndex > StartIndex)
            {
                return WriteIndex - StartIndex;
            }
            else if (WriteIndex == StartIndex)
            {
                return Capacity;
            }
            else // if (WriteIndex < StartIndex)
            {
                return (WriteIndex + Capacity) - StartIndex;
            }
        }
    }
    public int Capacity => values.Length;

    public int StartIndex { get; private set; } = 0;
    private int WriteIndex { get; set; } = BUFFER_EMPTY_WRITE_IDX;
    public int ReadIndex { get; private set; } = 0;
    public bool AtStart() { return IsEmpty() || StartIndex == ReadIndex; }
    public T First
    {
        get
        {
            if (IsEmpty())
            {
                throw new IndexOutOfRangeException("Attempting to access current value when the buffer is empty");
            }
            return values[StartIndex];
        }
    }
    public bool AtEnd() { return IsEmpty() || WriteIndex == ReadIndex; }
    public T End
    {
        get
        {
            if (IsEmpty())
            {
                throw new IndexOutOfRangeException("Attempting to access current value when the buffer is empty");
            }
            return values[WriteIndex];
        }
    }
    public T Value
    {
        get
        {
            if (IsEmpty())
            {
                throw new IndexOutOfRangeException("Attempting to access current value when the buffer is empty");
            }
            return values[ReadIndex];
        }
    }
    private int At(int idx)
    {
        int wrappedIdx = abs(idx) % Count;
        return ((StartIndex + wrappedIdx) % Capacity);
    }

    private int abs(int val) => val < 0 ? -val : val;
    private int IndexDelta(int lhs, int rhs)
    {
        if (IsEmpty()) throw new IndexOutOfRangeException("Trying to find index delta for an empty circular buffer");
        if (lhs < rhs) return rhs - lhs;
        else if (lhs == rhs) return Capacity; // Full!
        else return (rhs + Capacity) - lhs;
    }
    private int NextIndex(int idx)
    {
        return NextIndex(idx, 1);
    }
    private int NextIndex(int idx, int offset)
    {
        offset = abs(offset);
        return (idx + offset) % Capacity;
    }
    private int PrevIndex(int idx)
    {
        return PrevIndex(idx, 1);
    }
    private int PrevIndex(int idx, int offset)
    {
        offset = abs(offset) % Capacity;
        return (idx + Capacity - offset) % Capacity;
    }
    private bool IsIndexInRange(int idx)
    {
        if (IsEmpty() || idx < 0 || idx >= Capacity)
        {
            return false;
        }
        else if (StartIndex < PrevIndex(WriteIndex))
        {
            return StartIndex <= idx && idx <= PrevIndex(WriteIndex);
        }
        else if (StartIndex == PrevIndex(WriteIndex))
        {
            // Everything is in range cause we're full
            return true;
        }
        else //if (PrevIndex(WriteIndex) < StartIndex)
        {
            return StartIndex <= idx || idx <= PrevIndex(WriteIndex);
        }
    }

    public CircularBuffer<T> SeekNext()
    {
        if (IsEmpty())
        {
            throw new IndexOutOfRangeException("Attempting to seek to the next value in an empty circular buffer!");
            //return this;
        }

        int newIndex = ((ReadIndex + 1) % Count);
        if (newIndex != WriteIndex) ReadIndex = newIndex;
        return this;
    }
    public CircularBuffer<T> SeekPrev()
    {
        if (IsEmpty())
        {
            throw new IndexOutOfRangeException("Attempting to seek to the prev value in an empty circular buffer!");
            //return this;
        }

        if (Count != 0 && ReadIndex != StartIndex)
        {
            if (--ReadIndex < 0 && IsFilled())
            {
                ReadIndex = Count - 1;
            }
        }
        return this;
    }
    public CircularBuffer<T> SeekBegin()
    {
        ReadIndex = StartIndex;
        return this;
    }
    public CircularBuffer<T> SeekEnd()
    {
        if (IsEmpty())
        {
            throw new IndexOutOfRangeException("Attempting to seek to the end of the buffer, but the buffer is empty!");
            //return this;
        }
        ReadIndex = PrevIndex(WriteIndex, 1);
        return this;
    }
    public T Peek() { return Peek(0); }
    public T Peek(int offset)
    {
        if (IsEmpty())
        {
            throw new IndexOutOfRangeException("Can't peek into an empty circular buffer!");
        }
        if (!IsIndexInRange(ReadIndex))
        {
            throw new System.Exception("FUCK");
        }
        int wrappedOffset = abs(offset) % Count;
        int idx = ((ReadIndex + wrappedOffset) % Capacity);
        if (offset < 0)
        {
            idx = ((ReadIndex + Capacity - wrappedOffset) % Capacity);
        }
        return values[idx];
    }
    public T PeekStart()
    {
        if (IsEmpty())
        {
            throw new IndexOutOfRangeException("Attempting to peek into the buffer, but the buffer is empty!");
            //return this;
        }
        return values[ReadIndex];
    }
    public T PeekEnd()
    {
        if (IsEmpty())
        {
            throw new IndexOutOfRangeException("Attempting to peek into the buffer, but the buffer is empty!");
            //return this;
        }
        return values[PrevIndex(WriteIndex, 1)];
    }
    public T Pop()
    {
        if (IsEmpty())
        {
            throw new IndexOutOfRangeException("Can't pop any further; circular buffer is already empty!");
        }

        T retVal = values[StartIndex];
        if (AtStart())
        {
            ReadIndex = NextIndex(ReadIndex);
        }
        StartIndex = NextIndex(StartIndex);
        if (StartIndex == WriteIndex)
        {
            WriteIndex = BUFFER_EMPTY_WRITE_IDX;
        }

        return retVal;
    }
    public void Push(T item)
    {
        if (StartIndex == WriteIndex)
        {
            // We're full!
            if (NextIndex(ReadIndex) == StartIndex)
            {
                ReadIndex = NextIndex(ReadIndex);
            }
            StartIndex = NextIndex(StartIndex);
        }
        else if ((WriteIndex == -1) || NextIndex(ReadIndex) == WriteIndex)
        {
            // Cycle
            ReadIndex = NextIndex(ReadIndex);
        }
        values[IsEmpty() ? StartIndex : WriteIndex] = item;
        WriteIndex = NextIndex(IsEmpty() ? StartIndex : WriteIndex);
    }
    public void Push(params T[] items)
    {
        for (int i = 0; i < items.Length; ++i)
        {
            Push(items[i]);
        }
    }
    public void Clear()
    {
        WriteIndex = BUFFER_EMPTY_WRITE_IDX;
        StartIndex = 0;
        ReadIndex = 0;
    }
    public bool IsFilled()
    {
        return Capacity == Count;
    }
    public bool IsEmpty()
    {
        return WriteIndex == BUFFER_EMPTY_WRITE_IDX;
    }

    public void ForEach(Action<T> func)
    {
        for (int i = 0; i < Count; ++i)
        {
            int idx = NextIndex(StartIndex, i);
            func.Invoke(values[idx]);
            if (NextIndex(idx) == WriteIndex)
            {
                return;
            }
        }
    }

    void AddRange(IEnumerable<T> collection)
    {
        var iter = collection.GetEnumerator();
        int DEBUG_StopGap = 0;
        while (iter.MoveNext())
        {
            if (++DEBUG_StopGap > 10000) throw new System.Exception("infinite loop");
            Push(iter.Current);
        }
    }

    private readonly int MaxToStringDepth = 30;
    private StringBuilder toStringBuilder = new StringBuilder();
    public override string ToString()
    {
        toStringBuilder.Clear();
        toStringBuilder.Append("Absolute Order: { ");
        for (int i = 0; i < Capacity; ++i)
        {
            char quoteL = ' ';
            char quoteR = ' ';
            if (i == ReadIndex)
            {
                quoteR = '∞';
            }

            if (i == WriteIndex)
            {
                if (i == StartIndex)
                {
                    quoteL = '=';
                }
                else
                {
                    quoteL = '』';
                }
            }
            else if (i == StartIndex)
            {
                quoteL = (IsEmpty() ? 'X' : '『');
            }

            toStringBuilder.AppendFormat(i == 0 ? "{0}{1}{2}" : ", {0}{1}{2}", quoteL, (values[i].Equals(DefaultValue) ? "-" : values[i].ToString()), quoteR);
            if (i == MaxToStringDepth - 1)
            {
                toStringBuilder.Append("...");
                break;
            }
        }

        toStringBuilder.Append(" }\nRelative Order: [ ");
        if (IsEmpty())
        {
            toStringBuilder.Append("Empty Buffer ]");
            return toStringBuilder.ToString();
        }
        int idx = StartIndex;
        int DEBUG_StopGap = 0;
        do
        {
            if (++DEBUG_StopGap > 10000) throw new System.Exception("infinite loop");
            char quoteL = ' ';
            char quoteR = ' ';
            if (idx == ReadIndex)
            {
                quoteR = '∞';
            }
            if (idx == WriteIndex)
            {
                if (idx == StartIndex)
                {
                    quoteL = '=';
                }
                else
                {
                    quoteL = '』';
                }
            }
            else if (idx == StartIndex)
            {
                quoteL = '『';
            }

            toStringBuilder.AppendFormat(idx == StartIndex ? "{0}{1}{2}" : ", {0}{1}{2}", quoteL, (!IsIndexInRange(idx) || values[idx].Equals(DefaultValue) ? "-" : values[idx].ToString()), quoteR);
            if (idx == MaxToStringDepth - 1)
            {
                toStringBuilder.Append("...");
                break;
            }
            idx = NextIndex(idx);
        } while (idx != WriteIndex);
        toStringBuilder.Append(" ]");

        return toStringBuilder.ToString();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Core.AbiEncoding.Encoders
{    
    // TODO: attempt to share code between this untyped encoder and the typed/generic encoder

    public class FixedArrayEncoderNonGeneric : IAbiTypeEncoder
    {
        IAbiTypeEncoder _itemEncoder;
        IEnumerable<object> _val;

        public FixedArrayEncoderNonGeneric(IAbiTypeEncoder itemEncoder)
        {
            _itemEncoder = itemEncoder;
        }

        public AbiTypeInfo TypeInfo { get; private set; }

        public void SetTypeInfo(AbiTypeInfo info)
        {
            if (info.Category != SolidityTypeCategory.FixedArray)
            {
                throw EncoderUtil.CreateUnsupportedTypeEncodingException(info);
            }

            TypeInfo = info;
        }

        public void SetValue(object val) => _val = (val as IEnumerable).Cast<object>();


        private delegate void WalkElementAction(ref AbiDecodeBuffer buffer, Array innerMostArray, int index);

        public void DecodeObject(ref AbiDecodeBuffer buffer, out object result)
        {
            // If we have no elements, no work needs to be done.
            if (TypeInfo.ArrayDimensionSizes.Length == 0)
            {
                result = Array.Empty<object>();
                return;
            }

            // Create our initial array.
            var items = (Array)ArrayExtensions.CreateJaggedArray(TypeInfo.ArrayItemInfo.ClrType, TypeInfo.ArrayDimensionSizes);
            
            // Create a variable to track our position.
            int[] decodingPosition = new int[TypeInfo.ArrayDimensionSizes.Length];

            // Loop for each element to index.
            bool reachedEnd = false;
            while (!reachedEnd)
            {
                // Define the parent array to resolve for this element.
                Array innerMostArray = items;

                // Increment our decoding position.
                bool incrementing = true;
                for (int x = 0; x < decodingPosition.Length; x++)
                {
                    // If this isn't the final index (inner most array index), then it's an index to another array.
                    if (x < decodingPosition.Length - 1)
                    {
                        innerMostArray = (Array)innerMostArray.GetValue(decodingPosition[x]);
                    }
                    else
                    {
                        // We've resolved the element to index.
                        _itemEncoder.DecodeObject(ref buffer, out var item);
                        innerMostArray.SetValue(item, decodingPosition[x]);
                    }

                    // Increment the index for this dimension
                    if (incrementing)
                    {
                        // Increment our position.
                        decodingPosition[x]++;

                        // Determine if we need to carry a digit.
                        if (decodingPosition[x] >= TypeInfo.ArrayDimensionSizes[x])
                        {
                            // Reset the digit, we will carry over to the next.
                            decodingPosition[x] = 0;
                        }
                        else
                        {
                            incrementing = false;
                        }
                    }
                }

                // If we incremented all digits and still have increment flag set, we overflowed our last element, so we reached the end
                reachedEnd = incrementing;
            }

            // Set our result
            result = items;
        }

        void ValidateArrayLength()
        {
            void Validate(IEnumerable<object> val, int[] arrayDimensionSizes, int i)
            {
                var actualCount = val.Count();
                if (actualCount != arrayDimensionSizes[i])
                {
                    throw new ArgumentOutOfRangeException($"Fixed size array type '{TypeInfo.SolidityName}' needs exactly {arrayDimensionSizes[i]} items, was given {actualCount}");

                }

                if (i < arrayDimensionSizes.Length - 1)
                {
                    foreach (var subItem in val)
                    {
                        Validate((subItem as IEnumerable).Cast<object>(), arrayDimensionSizes, i + 1);
                    }
                }
            }

            Validate(_val, TypeInfo.ArrayDimensionSizes, 0);
        }

        public void Encode(ref AbiEncodeBuffer buffer)
        {
            ValidateArrayLength();

            // If we have no elements, no work needs to be done.
            if (TypeInfo.ArrayDimensionSizes.Length == 0)
            {
                return;
            }

            // Create a variable to track our position.
            int[] encodingPosition = new int[TypeInfo.ArrayDimensionSizes.Length];

            // Loop for each element to index.
            bool reachedEnd = false;
            while (!reachedEnd)
            {
                // Define the parent array to resolve for this element.
                Array innerMostArray = (Array)_val;

                // Increment our decoding position.
                bool incrementing = true;
                for (int x = 0; x < encodingPosition.Length; x++)
                {
                    // If this isn't the final index (inner most array index), then it's an index to another array.
                    if (x < encodingPosition.Length - 1)
                    {
                        innerMostArray = (Array)innerMostArray.GetValue(encodingPosition[x]);
                    }
                    else
                    {
                        // We've resolved the element to index.
                        _itemEncoder.SetValue(innerMostArray.GetValue(encodingPosition[x]));
                        _itemEncoder.Encode(ref buffer);
                    }

                    // Increment the index for this dimension
                    if (incrementing)
                    {
                        // Increment our position.
                        encodingPosition[x]++;

                        // Determine if we need to carry a digit.
                        if (encodingPosition[x] >= TypeInfo.ArrayDimensionSizes[x])
                        {
                            // Reset the digit, we will carry over to the next.
                            encodingPosition[x] = 0;
                        }
                        else
                        {
                            incrementing = false;
                        }
                    }
                }

                // If we incremented all digits and still have increment flag set, we overflowed our last element, so we reached the end
                reachedEnd = incrementing;
            }
        }

        public void EncodePacked(ref Span<byte> buffer)
        {
            ValidateArrayLength();
            foreach (var item in _val)
            {
                _itemEncoder.SetValue(item);
                _itemEncoder.EncodePacked(ref buffer);
            }
        }

        public int GetEncodedSize()
        {
            int totalArraySize = 1;
            foreach (var dim in TypeInfo.ArrayDimensionSizes)
            {
                totalArraySize *= dim;
            }

            int len = _itemEncoder.GetEncodedSize() * totalArraySize;
            return len;
        }

        public int GetPackedEncodedSize()
        {
            int totalArraySize = 1;
            foreach (var dim in TypeInfo.ArrayDimensionSizes)
            {
                totalArraySize *= dim;
            }

            int len = _itemEncoder.GetPackedEncodedSize() * totalArraySize;
            return len;
        }



    }

}

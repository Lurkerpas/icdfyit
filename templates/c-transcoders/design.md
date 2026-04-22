# General
C code shall conform to C17 standard.
C code shall be portable - no compiler specific extensions can be used.
C code shall consist of small, modular functions.
C code shall be human readable:
- descriptive names,
- low nesting,
- low number of parameters,
- low cyclomatic complexity (unless a non-nested switch construct is used).
C code shall not use dynamic memory allocation, all buffers must be provided preallocated by the user.
C code shall, whenever possible, provide constants indicating the required sizes of buffers, or defines (that can be used at compile time) to calculate them.
Functions shall verify their inputs.
Error handling shall be via return value with additional error code parameter:
- function returns true if succesfull, false otherwise
- error code is returned via uint32_t* errorCode parameter, present last. 
Type names shall use PascalCase (except for prefix).
Function names shall use snake_case (except for prefix).
All declarations shall have accompanying Doxygen compliant documentation.
All function definitions shall contain comments, at least one comment for 3 lines of code (to be compliant with strict embedded coding standards).
Comments shall focus on explaining why and how things are done, not just repeating the code.

# Utility functions for C transcoders.
Utility functions shall be provided in a library that consists of 2 files:
- icdfyit-transcoders.h - with type and function declarations,
- icdfyit-transcoders.c - with function definitions.
All types shall have prefix IcdUF_.
All functional shall have prefix icduf_.
Functions and types supporting the following capabilities shall be implemented:
- maintaining a static byte buffer to which transcoded data can be appended or read from for transcoding,
- transcoding (encoding and decoding) of:
-- unsigned integers of 8, 16, 24 and 32 bits,
-- signed integers of 8, 16, 24, and 32 bits.
-- floats of 32 bits.
-- booleans of 8, 16, 24 and 32 bits.

Separate functions for big and little endian shall be provided.

# Templates for C transcoders.
2 templates:
- one for C header file,
- one for C source file.
Header shall include type and constant declarations, as well as function declarations.
Source shall include function definitions.

All types shall have prefix IcdT_.
All functional shall have prefix icdt_.
Types shall represent used Data Types and Packet Types from the model.
Only Data Types used by Parameters used in Packet Types shall be taken into account.
Functions for transcoding and decoding Data Types and Packet Types shall be provided.
Utility Functions shall be used for low-level operations.
Constraints (min/max value, range, constant pattern) shall be verified, and standardized errors shall be returned.
The list of possible error values shall be static, not dependent on the data model.
For decoding Packet Types, the following support function shall be implemented for each headers type:
- function takes header field values as input
- function checks packet type indicator values
- function returns the exact Packet Type.
The aim of this function is to be able to precisely decide which Packet Type is to be decoded. Example use (naming is non-binding, only illustrative):
'''
IcdUF_ByteBuffer buffer = receive_data();
IcdT_PacketType type = icdt_header_x_get_type(buffer, header_param1, header_param2, &errNo);
switch (type)
{
    case IcdT_PacketType_WriteRegister:
    {
        IcdT_WriteRegister packet;
        icdt_writeregister_decode(buffer, &packet, &errNo); 
    }
    break;
}
'''
If Data Model contains types that cannot be transcoded using the provided utility functions (e.g., 11 bit integer), error shall be produced during template generation.

---
title: Hooking to object events/scripts
description: How to hook to object events and scripts, allowing you to still run the original code along with custom code, and also edit code that normally can't decompile.
sidebar:
  order: 2
---
# Hooking

## What is hooking?
Hooking is a GS2ML feature that is ~~stolen from~~ based on [GMML](https://github.com/cgytrus/gmml)'s hooker functions, as GS2ML is intended to be a replacement for GMML. It allows you to change code originally in the game, while also being able to call the ORIGINAL code like a function, meaning you can edit scripts that you normally couldn't compile with UndertaleModTool.

## How does hooking work? (GML example)
Let's say you have a function that is in the unmodified files of the game you are editing:
```gml
function my_cool_function(argument0, argument1){
  foo = argument0
  foofoo = argument1
  important_variable = {my_cool_struct_value:1}
  return [foo, foofoo]
}
```
You want the function to ALWAYS return \[foo, 0\] instead of \[foo, foofoo\].

You COULD just copy the original code, make changes to it, and replace the code with the modified code, but it's much more clean to just hook.  
Plus, in this case important_variable is being set to a STRUCT, which, as of the time this is being written, UndertaleModLib does not support recompiling, meaning you cannot make changes to this function by copying it.

SO, let's HOOK the function instead.  
When you hook a function, the original code for the function is moved to a SEPARATE function. So you would end up with
```gml
function my_cool_function(argument0, argument1){
  //Your custom code here.
}

function my_cool_function_original(argument0, argument1){
  var foo = argument0
  var foofoo = argument1
  important_variable = {my_cool_struct_value:1}
  return [foo, foofoo]
}
```

So, let's put this to good use. Let's call the original function using the special syntax `#orig#()`.
```gml
#orig#()
return [foo, foofoo]
```

GS2ML will replace any instace of `#orig#` with the name of the function that contains the ORIGINAL code.
```gml
#orig#()
//turns into
my_cool_function_original()
```

Currently, however, this code will return an error. This is because, in the original function, foo and foofoo are LOCAL variables, NOT object variables, meaning they only have a value while `my_cool_function_original` is being run.   
So, we need to make use of what `#orig#` returns, because, like I said, we are calling the original code as a function, meaning we can recieve what it returns.  
```gnl
var returned_value = #orig#()
//Set the second value in the returned array, which was originally "foofoo", to 0.
returned_value[1] = 0
return returned_value
```

Now we have only one more error to fix. Because we are calling the original code like a function, we need to pass on the ARGUMENTS of the function.
So we will call `var returned_value = #orig#(argument0, argument1)`.
The final code will look like this:
```gml
var returned_value = #orig#(argument0, argument1)
//Set the second value in the returned array, "foofoo", to 0.
returned_value[1] = 0
return returned_value
```

And when GS2ML compiles it, it will look like this:
```gml
function my_cool_function(argument0, argument1){
  var returned_value = my_cool_function_original(argument0, argument1)
  returned_value[1] = 0
  return returned_value
}

function my_cool_function_original(argument0, argument1){
  var foo = argument0
  var foofoo = argument1
  important_variable = {my_cool_struct_value:1}
  return [foo, foofoo]
}
```

# How to do it in C#
Now, how do we do the C# side of this?  

Well, the syntax for hooking a FUNCTION/SCRIPT is  
`data.HookFunction("function_name", "//The code you want to run")`  
Be sure NOT to include the `gml_Script_` at the beginning of the function name. So don't say `"gml_Script_function_name"`, only say `"function_name"`.

The syntax for hooking OBJECT EVENTS is also similar.  
`data.HookCode("gml_Object_obj_my_object_Create_0", "//The code you want to run")`  
In this case you DO want to include `gml_Object_` at the beginning. So don't just do `"obj_my_object_Create_0"`, instead do `"gml_Object_obj_my_object_Create_0"`.

Keep in mind that the `data` variable in this case is the UndertaleData that you are editing.  


Congratulations! You now know how to hook functions and object events!

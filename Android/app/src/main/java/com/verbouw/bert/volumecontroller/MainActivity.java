package com.verbouw.bert.volumecontroller;

import android.app.Activity;
import android.content.SharedPreferences;
import android.os.Handler;
import android.os.Parcelable;
import android.support.design.widget.Snackbar;
import android.support.design.widget.TextInputEditText;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.util.SparseArray;
import android.view.View;
import android.view.inputmethod.InputMethodManager;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;

import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.Response;
import com.android.volley.VolleyError;
import com.android.volley.toolbox.JsonArrayRequest;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.UnsupportedEncodingException;

public class MainActivity extends AppCompatActivity {

    private LinearLayout volumeControllerContainer;
    private ScrollView scrollView;
    private LinearLayout inputLayout;
    private EditText ipTextBox;
    private EditText portTextBox;
    private int interval = 250;
    private Handler handler;
    private SharedPreferences preferences;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        volumeControllerContainer = this.findViewById(R.id.vcContainer);
        scrollView = this.findViewById(R.id.scrollViewer);
        ipTextBox = this.findViewById(R.id.ipaddress);
        portTextBox = this.findViewById(R.id.port);
        inputLayout = this.findViewById(R.id.inputLayout);
        handler = new Handler();

        preferences = getSharedPreferences("label", 0);
        String ip = preferences.getString("ipAddress", "");
        String port = preferences.getString("port", "");
        Variables.ipAddress = ip;
        Variables.port = port;
        ipTextBox.setText(ip);
        portTextBox.setText(port);
    }

    private void loadVolumeControllers(JSONArray data) {
        RemoveVolumeControllers(data);
        AddVolumeControllers(data);
    }

    private void AddVolumeControllers(JSONArray data) {
        for (int i = 0; i < data.length(); i++) {
            try {
                JSONObject object = data.getJSONObject(i);
                if(!viewContainsPid(object.getInt("Pid"))){
                    volumeControllerContainer.addView(new VolumeController(this, object.getBoolean("IsMuted"), object.getInt("Volume"), object.getString("Name"), object.getInt("Pid")));
                }else{
                    EditVolumeController(object);
                }
            }
            catch (JSONException e) {
            }
        }
    }

    private void EditVolumeController(JSONObject object) {
        for(int i=0; i<volumeControllerContainer.getChildCount();i++){
            try {
                if(((VolumeController)volumeControllerContainer.getChildAt(i)).getPid() == object.getInt("Pid")){
                    VolumeController controller = (VolumeController)volumeControllerContainer.getChildAt(i);
                    controller.setVolume(object.getInt("Volume"));
                    controller.setIsMuted(object.getBoolean("IsMuted"));
                    controller.setName(object.getString("Name"));
                    controller.initializeValues();
                }
            } catch (JSONException e) {
            }
        }
    }

    private void RemoveVolumeControllers(JSONArray data) {
        for(int i=0; i<volumeControllerContainer.getChildCount();i++){
            if(!dataContainsChildPid((VolumeController)volumeControllerContainer.getChildAt(i), data)){
                volumeControllerContainer.removeViewAt(i);
            }
        }
    }

    private boolean dataContainsChildPid(VolumeController child, JSONArray data) {
        for (int i = 0; i < data.length(); i++) {
            try {
                JSONObject object = data.getJSONObject(i);
                if(object.getInt("Pid") == child.getPid()){
                    return true;
                }
            }
            catch (JSONException e) {
            }
        }
        return false;
    }

    private boolean viewContainsPid(int pid) {
        for(int i=0; i<volumeControllerContainer.getChildCount();i++){
            if(((VolumeController)volumeControllerContainer.getChildAt(i)).getPid() == pid){
                return true;
            }
        }
        return false;
    }

    @Override
    protected void onResume(){
        super.onResume();
        apiLoop.run();
    }

    @Override
    public void onStop(){
        super.onStop();
        handler.removeCallbacks(apiLoop);
    }

    Runnable apiLoop = new Runnable() {
        @Override
        public void run() {
            try {
                getApiData(); //this function can change value of interval.
            } finally {
                // 100% guarantee that this always happens, even if
                // your update method throws an exception
                handler.postDelayed(apiLoop, interval);
            }
        }
    };

    private void getApiData() {
        // Instantiate the RequestQueue.
        RequestQueue queue = Volley.newRequestQueue(this);
        String url ="http://"+Variables.ipAddress+":"+Variables.port+"/get/";

        // Request a string response from the provided URL.
        JsonArrayRequest jsonArrayRequest = new JsonArrayRequest(Request.Method.GET, url, null,
                new Response.Listener<JSONArray>() {
                    @Override
                    public void onResponse(JSONArray response) {
                        inputLayout.setVisibility(View.INVISIBLE);
                        scrollView.setVisibility(View.VISIBLE);
                        loadVolumeControllers(response);
                    }
                }, new Response.ErrorListener() {
            @Override
            public void onErrorResponse(VolleyError error) {
                handler.removeCallbacks(apiLoop);
                volumeControllerContainer.removeAllViews();
                scrollView.setVisibility(View.INVISIBLE);
                inputLayout.setVisibility(View.VISIBLE);
            }
        });
        // Add the request to the RequestQueue.
        queue.add(jsonArrayRequest);
    }

    public void retryButtonClick(View view) {
        hideKeyboard(this);
        Variables.ipAddress = ipTextBox.getText().toString();
        Variables.port = portTextBox.getText().toString();
        preferences.edit().putString("ipAddress", Variables.ipAddress).commit();
        preferences.edit().putString("port", Variables.port).commit();
        apiLoop.run();
    }

    public static void hideKeyboard(Activity activity) {
        InputMethodManager imm = (InputMethodManager) activity.getSystemService(Activity.INPUT_METHOD_SERVICE);
        //Find the currently focused view, so we can grab the correct window token from it.
        View view = activity.getCurrentFocus();
        //If no view currently has focus, create a new one, just so we can grab a window token from it
        if (view == null) {
            view = new View(activity);
        }
        imm.hideSoftInputFromWindow(view.getWindowToken(), 0);
    }
}
